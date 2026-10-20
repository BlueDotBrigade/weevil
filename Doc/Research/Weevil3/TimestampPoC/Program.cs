// Proof of concept for #1008: keep each record's timestamp in memory, or parse it when needed?
// Standalone: it does not reference Weevil. WeevilCopies/ holds verbatim copies of the classes it needs.

#nullable enable

namespace Weevil.TimestampPoC;

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BlueDotBrigade.Weevil.Data;

internal static class Program
{
	public static int Main(string[] args)
	{
		Arguments arguments;
		try
		{
			arguments = Arguments.Parse(args);
		}
		catch (ArgumentException e)
		{
			Console.Error.WriteLine(e.Message);
			Arguments.PrintHelp();
			return 2;
		}

		if (arguments.ShowHelp)
		{
			Arguments.PrintHelp();
			return 0;
		}

		var path = arguments.FilePath ?? SyntheticLog.EnsureExists(arguments);
		var log = IndexedLog.Load(path);

		if (log.Count < 100)
		{
			Console.Error.WriteLine($"Too few records to measure: {log.Count}. Is this a Weevil TSV log?");
			return 1;
		}

		var inputs = Inputs.Create(log, arguments.FilterPercent);

		VariantResult[] variants = arguments.Ways
			.Select(way => way switch
			{
				Way.Column => Measure("Column", new ColumnSource(log), log, inputs, arguments),
				Way.Plugin => Measure("Plugin parse", new PluginParseSource(log), log, inputs, arguments),
				Way.Span => Measure("Span parse", new SpanParseSource(log), log, inputs, arguments),
				Way.Fast => Measure("Fast parse", new FastParseSource(log), log, inputs, arguments),
				_ => throw new InvalidOperationException($"Unknown way: {way}"),
			})
			.ToArray();

		var report = Report.Create(args, arguments, log, variants);
		Console.WriteLine();
		Console.Write(report);

		if (arguments.ReportPath is not null)
		{
			File.WriteAllText(arguments.ReportPath, report);
			Console.Error.WriteLine($"Report saved: {Path.GetFullPath(arguments.ReportPath)}");
		}

		return variants.All(v => v.HasSameAnswersAs(variants[0])) ? 0 : 1;
	}

	private static VariantResult Measure<T>(string name, T source, IndexedLog log, Inputs inputs, Arguments arguments)
		where T : struct, ITimestampSource
	{
		Console.Error.WriteLine($"Measuring: {name}...");
		var runs = arguments.Runs;

		return new VariantResult(name,
		[
			Measure(() => Workloads.Insights(source, log), runs),
			Measure(() => Workloads.ElapsedFilter(source, inputs.FilterResults), runs),
			Measure(() => Workloads.ElapsedFilter(source, inputs.AllRecords), runs),
			Measure(() => Workloads.GoToTimestamp(source, inputs.Targets, log.Count), runs),
			Measure(() => Workloads.RowsOnScreen(source, inputs.ScreenPositions), runs),
			Measure(() => Workloads.ReadAllInParallel(source, log.Count, arguments.Threads), runs),
		]);
	}

	private static Measurement Measure<TResult>(Func<TResult> workload, int runs)
		where TResult : notnull
	{
		// The first run warms up the JIT, and its answer is compared across the ways of reading timestamps.
		TResult answer = workload();

		var milliseconds = new double[runs];
		for (var run = 0; run < runs; run++)
		{
			var stopwatch = Stopwatch.StartNew();
			workload();
			milliseconds[run] = stopwatch.Elapsed.TotalMilliseconds;
		}

		Array.Sort(milliseconds);
		return new Measurement(answer, milliseconds[runs / 2]);
	}
}

internal enum Way
{
	Column,
	Plugin,
	Span,
	Fast,
}

internal sealed record Measurement(object Answer, double Milliseconds);

internal sealed record VariantResult(string Name, Measurement[] Measurements)
{
	public bool HasSameAnswersAs(VariantResult other) =>
		this.Measurements.Zip(other.Measurements).All(pair => pair.First.Answer.Equals(pair.Second.Answer));
}

internal static class Report
{
	private const double TwentyGigabytes = 20_000_000_000;
	private const double SixtyFourGigabytes = 64_000_000_000;

	// Decision rule from the #988 plan, step 2.
	private const double MaxExtraSecondsAtOpen = 5;
	private const double MaxElapsedFilterSeconds = 10;

	// Indexes into VariantResult.Measurements, in the order Program.Measure adds them.
	private const int Insights = 0;
	private const int FilterSubset = 1;
	private const int FilterAll = 2;
	private const int GoTo = 3;
	private const int Rows = 4;
	private const int ParallelRead = 5;

	public static string Create(string[] args, Arguments arguments, IndexedLog log, VariantResult[] variants)
	{
		var w = new StringWriter(CultureInfo.InvariantCulture);
		var scale = TwentyGigabytes / log.FileBytes;
		var uiShare = (double)log.IsUiThread.Count(x => x) / log.Count;

		string[] workloadNames =
		[
			"Insights at open",
			$"`@Elapsed` filter, {arguments.FilterPercent}% of records",
			"`@Elapsed` filter, all records",
			$"Go to timestamp, ×{Inputs.TargetCount:N0}",
			$"Rows on screen, ×{Inputs.ScreenCount:N0} screens",
			$"Read every timestamp once, {arguments.Threads} threads",
		];

		w.WriteLine("# Timestamp proof of concept (#1008)");
		w.WriteLine();
		w.WriteLine("| | |");
		w.WriteLine("|---|---|");
		w.WriteLine($"| Options | `{(args.Length == 0 ? "(defaults)" : string.Join(' ', args))}` |");
		w.WriteLine($"| Log | {(arguments.FilePath is null ? "Synthetic" : "Real")}, {log.FileBytes / 1_000_000.0:N0} MB |");
		w.WriteLine($"| Records | {log.Count:N0}, averaging {(double)log.FileBytes / log.Count:N0} bytes |");
		w.WriteLine($"| UI-thread records | {uiShare:P1} |");
		w.WriteLine($"| Indexing, which also fills the column | {log.IndexingTime.TotalSeconds:N1} s |");
		w.WriteLine($"| Machine | {Environment.ProcessorCount} logical processors, {RuntimeInformation.OSDescription}, {RuntimeInformation.FrameworkDescription} |");
		w.WriteLine($"| Timing | Median of {arguments.Runs} {(arguments.Runs == 1 ? "run" : "runs")}; one thread unless stated |");

		w.WriteLine();
		w.WriteLine("## Measured (ms)");
		WriteTable(w, variants, workloadNames, [Insights, FilterSubset, FilterAll, GoTo, Rows, ParallelRead],
			m => m.Milliseconds.ToString("N0", CultureInfo.InvariantCulture));

		w.WriteLine();
		w.WriteLine("## Scaled to a 20 GB log (s)");
		w.WriteLine();
		w.WriteLine("Only the workloads that read every record grow with the log. Records of the same average size are assumed.");
		WriteTable(w, variants, workloadNames, [Insights, FilterSubset, FilterAll, ParallelRead],
			m => (m.Milliseconds * scale / 1000).ToString("N1", CultureInfo.InvariantCulture));

		w.WriteLine();
		w.WriteLine("## Per action (µs)");
		w.WriteLine();
		w.WriteLine("These hardly grow with the log: a search in a 20 GB log takes about 10 more steps.");
		w.WriteLine();
		w.WriteLine($"| Action | {string.Join(" | ", variants.Select(v => v.Name))} |");
		w.WriteLine($"|---|{string.Concat(variants.Select(_ => "---|"))}");
		w.WriteLine($"| Go to timestamp | {PerAction(variants, GoTo, Inputs.TargetCount)} |");
		w.WriteLine($"| Elapsed times for one screen of 50 rows | {PerAction(variants, Rows, Inputs.ScreenCount)} |");

		var sixtyFourScale = SixtyFourGigabytes / log.FileBytes;
		w.WriteLine();
		w.WriteLine("## Memory for the column");
		w.WriteLine();
		w.WriteLine("| Log | Records | Column |");
		w.WriteLine("|---|---|---|");
		w.WriteLine($"| This log | {log.Count:N0} | {log.Count * 8 / 1_000_000.0:N0} MB |");
		w.WriteLine($"| 20 GB | {log.Count * scale:N0} | {log.Count * scale * 8 / 1_000_000_000:N1} GB |");
		w.WriteLine($"| 64 GB | {log.Count * sixtyFourScale:N0} | {log.Count * sixtyFourScale * 8 / 1_000_000_000:N1} GB |");

		w.WriteLine();
		w.WriteLine("## Decision rule");
		w.WriteLine();
		w.WriteLine($"Keep the column, unless parsing on demand adds under {MaxExtraSecondsAtOpen} s to opening a 20 GB log and keeps an `@Elapsed` filter under {MaxElapsedFilterSeconds} s. Insights run on one thread today.");
		w.WriteLine();
		w.WriteLine($"| Way | Extra at open | `@Elapsed` filter, all records | Within the rule? | Reading every timestamp, {arguments.Threads} threads |");
		w.WriteLine("|---|---|---|---|---|");

		VariantResult column = variants[0];
		string? fastEnough = null;
		foreach (VariantResult variant in variants.Skip(1))
		{
			var extraAtOpen = (variant.Measurements[Insights].Milliseconds - column.Measurements[Insights].Milliseconds) * scale / 1000;
			var elapsedFilter = variant.Measurements[FilterAll].Milliseconds * scale / 1000;
			var parallelRead = variant.Measurements[ParallelRead].Milliseconds * scale / 1000;
			var isWithinRule = extraAtOpen < MaxExtraSecondsAtOpen && elapsedFilter < MaxElapsedFilterSeconds;
			fastEnough ??= isWithinRule ? variant.Name : null;

			w.WriteLine($"| {variant.Name} | +{extraAtOpen:N1} s | {elapsedFilter:N1} s | {(isWithinRule ? "Yes" : "No")} | {parallelRead:N1} s |");
		}

		w.WriteLine();
		if (variants.Length == 1)
		{
			w.WriteLine("**Answer:** none: only the column was measured.");
		}
		else
		{
			w.WriteLine(fastEnough is null
				? "**Answer:** keep the column. No way of parsing on demand is within the rule."
				: $"**Answer:** parsing on demand is fast enough with \"{fastEnough}\". The column can go.");
		}

		var mismatches = variants.Skip(1).Where(v => !v.HasSameAnswersAs(column)).Select(v => v.Name).ToList();
		w.WriteLine();
		w.WriteLine(mismatches.Count == 0
			? "All ways gave the same answers."
			: $"**Warning:** different answers from: {string.Join(", ", mismatches)}. The timings are not comparable.");

		return w.ToString();
	}

	private static string PerAction(VariantResult[] variants, int workload, int actions) =>
		string.Join(" | ", variants.Select(v => (v.Measurements[workload].Milliseconds * 1000 / actions).ToString("N1", CultureInfo.InvariantCulture)));

	private static void WriteTable(TextWriter w, VariantResult[] variants, string[] workloadNames, int[] workloads, Func<Measurement, string> format)
	{
		w.WriteLine();
		w.WriteLine($"| Workload | {string.Join(" | ", variants.Select(v => v.Name))} |");
		w.WriteLine($"|---|{string.Concat(variants.Select(_ => "---|"))}");

		foreach (var i in workloads)
		{
			w.WriteLine($"| {workloadNames[i]} | {string.Join(" | ", variants.Select(v => format(v.Measurements[i])))} |");
		}
	}
}

/// <summary>
/// The workloads that read timestamps. Each is generic, so the JIT compiles it once per way of reading timestamps.
/// </summary>
internal static class Workloads
{
	private static readonly long TimeGapThreshold = TimeSpan.FromSeconds(1).Ticks;
	private static readonly long ElapsedThreshold = TimeSpan.FromMilliseconds(500).Ticks;

	internal readonly record struct InsightsAnswer(long TickSum, int OutOfOrder, long BiggestAnomaly, int TimeGaps);

	internal readonly record struct ElapsedAnswer(int Matches, long ElapsedSum);

	/// <summary>
	/// Two insights run after every open: out-of-order timestamps over every record,
	/// then time gaps between UI-thread records. Each is its own pass, as in Weevil.
	/// </summary>
	public static InsightsAnswer Insights<T>(T source, IndexedLog log)
		where T : struct, ITimestampSource
	{
		long tickSum = 0;
		var outOfOrder = 0;
		long biggestAnomaly = 0;

		var previous = source.Ticks(0);
		tickSum += previous;

		for (var i = 1; i < log.Count; i++)
		{
			var ticks = source.Ticks(i);
			unchecked
			{
				tickSum += ticks;
			}

			if (ticks < previous)
			{
				outOfOrder++;
				biggestAnomaly = Math.Max(biggestAnomaly, previous - ticks);
			}

			previous = ticks;
		}

		var timeGaps = 0;
		var previousUi = long.MinValue;

		for (var i = 0; i < log.Count; i++)
		{
			if (log.IsUiThread[i])
			{
				var ticks = source.Ticks(i);
				if (previousUi != long.MinValue && ticks - previousUi > TimeGapThreshold)
				{
					timeGaps++;
				}

				previousUi = ticks;
			}
		}

		return new InsightsAnswer(tickSum, outOfOrder, biggestAnomaly, timeGaps);
	}

	/// <summary>
	/// An `@Elapsed` filter compares the time between consecutive records of the previous results.
	/// </summary>
	public static ElapsedAnswer ElapsedFilter<T>(T source, int[] previousResults)
		where T : struct, ITimestampSource
	{
		var matches = 0;
		long elapsedSum = 0;
		var previous = source.Ticks(previousResults[0]);

		for (var i = 1; i < previousResults.Length; i++)
		{
			var ticks = source.Ticks(previousResults[i]);
			var elapsed = ticks - previous;
			unchecked
			{
				elapsedSum += elapsed;
			}

			if (elapsed > ElapsedThreshold)
			{
				matches++;
			}

			previous = ticks;
		}

		return new ElapsedAnswer(matches, elapsedSum);
	}

	/// <summary>
	/// Go to the first record at or after a timestamp: a binary search over every record.
	/// </summary>
	public static long GoToTimestamp<T>(T source, long[] targets, int count)
		where T : struct, ITimestampSource
	{
		long indexSum = 0;

		foreach (var target in targets)
		{
			int low = 0, high = count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (source.Ticks(middle) < target)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			indexSum += low;
		}

		return indexSum;
	}

	/// <summary>
	/// The elapsed-time column for the 50 rows on screen, at many scroll positions.
	/// </summary>
	public static long RowsOnScreen<T>(T source, int[] screenPositions)
		where T : struct, ITimestampSource
	{
		const int RowsPerScreen = 50;
		long elapsedSum = 0;

		foreach (var firstRow in screenPositions)
		{
			var previous = source.Ticks(firstRow - 1);
			for (var row = firstRow; row < firstRow + RowsPerScreen; row++)
			{
				var ticks = source.Ticks(row);
				unchecked
				{
					elapsedSum += ticks - previous;
				}

				previous = ticks;
			}
		}

		return elapsedSum;
	}

	/// <summary>
	/// Reads every record's timestamp once, split across threads: how fast a whole-log pass could parse in parallel.
	/// </summary>
	public static long ReadAllInParallel<T>(T source, int count, int threads)
		where T : struct, ITimestampSource
	{
		long tickSum = 0;

		Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, part =>
		{
			var first = (int)((long)count * part / threads);
			var end = (int)((long)count * (part + 1) / threads);
			long partSum = 0;

			for (var i = first; i < end; i++)
			{
				unchecked
				{
					partSum += source.Ticks(i);
				}
			}

			Interlocked.Add(ref tickSum, partSum);
		});

		return tickSum;
	}
}

internal sealed class Inputs
{
	public const int TargetCount = 1_000;
	public const int ScreenCount = 10_000;

	public required int[] AllRecords { get; init; }
	public required int[] FilterResults { get; init; }
	public required long[] Targets { get; init; }
	public required int[] ScreenPositions { get; init; }

	public static Inputs Create(IndexedLog log, int filterPercent)
	{
		var sample = new Random(7);
		var targets = new Random(11);
		var screens = new Random(13);

		var first = log.Ticks.Min();
		var last = log.Ticks.Max();

		var filterResults = Enumerable.Range(0, log.Count).Where(_ => sample.Next(100) < filterPercent).ToArray();

		return new Inputs
		{
			AllRecords = Enumerable.Range(0, log.Count).ToArray(),
			FilterResults = filterResults.Length >= 2 ? filterResults : [0, log.Count - 1],
			Targets = Enumerable.Range(0, TargetCount).Select(_ => first + (long)(targets.NextDouble() * (last - first))).ToArray(),
			ScreenPositions = Enumerable.Range(0, ScreenCount).Select(_ => screens.Next(1, log.Count - 50)).ToArray(),
		};
	}
}

/// <summary>
/// Reads one record's timestamp.
/// </summary>
internal interface ITimestampSource
{
	long Ticks(int record);
}

/// <summary>
/// Today's behaviour: the timestamp is stored, 8 bytes per record.
/// </summary>
internal readonly struct ColumnSource : ITimestampSource
{
	private readonly long[] _ticks;

	public ColumnSource(IndexedLog log) => _ticks = log.Ticks;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long Ticks(int record) => _ticks[record];
}

/// <summary>
/// Today's plugin contract: decode the record and parse it with Weevil's TSV parser, which builds a whole record.
/// </summary>
internal readonly struct PluginParseSource : ITimestampSource
{
	private static readonly TsvRecordParser Parser = new();
	private readonly IndexedLog _log;

	public PluginParseSource(IndexedLog log) => _log = log;

	public long Ticks(int record)
	{
		var content = Encoding.UTF8.GetString(_log.Bytes, _log.Starts[record], _log.Lengths[record]);
		Parser.TryParse(_log.LineNumbers[record], content, out IRecord parsed);
		return parsed.CreatedAt.Ticks;
	}
}

/// <summary>
/// A timestamp-only parse with the same rules as Weevil's TSV parser: `DateTime.TryParseExact` on the first field.
/// </summary>
internal readonly struct SpanParseSource : ITimestampSource
{
	private readonly IndexedLog _log;

	public SpanParseSource(IndexedLog log) => _log = log;

	public long Ticks(int record) =>
		TsvHeader.ParseTimestamp(_log.Bytes.AsSpan(_log.Starts[record], _log.Lengths[record]));
}

/// <summary>
/// A hand-written parse of the digits, falling back to the span parse for anything unexpected.
/// </summary>
internal readonly struct FastParseSource : ITimestampSource
{
	private readonly IndexedLog _log;

	public FastParseSource(IndexedLog log) => _log = log;

	public long Ticks(int record)
	{
		ReadOnlySpan<byte> line = _log.Bytes.AsSpan(_log.Starts[record], _log.Lengths[record]);
		return TsvHeader.TryParseTimestampFast(line, out var ticks) ? ticks : TsvHeader.ParseTimestamp(line);
	}
}

internal static class TsvHeader
{
	public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.FFFFF";

	private static readonly long[] FractionScale = [0, 1_000_000, 100_000, 10_000, 1_000, 100];

	/// <summary>
	/// Mirrors TsvRecordParser's decision: at least five tab-separated fields,
	/// a valid timestamp, and integer process and thread IDs.
	/// </summary>
	public static bool TryRead(ReadOnlySpan<byte> line, out long ticks, out bool isUiThread)
	{
		ticks = 0;
		isUiThread = false;

		var headerEnd = 0;
		for (var field = 0; field < 4; field++)
		{
			var tab = line[headerEnd..].IndexOf((byte)'\t');
			if (tab < 0)
			{
				return false;
			}

			headerEnd += tab + 1;
		}

		Span<char> buffer = stackalloc char[256];
		ReadOnlySpan<char> header = headerEnd <= buffer.Length
			? buffer[..Encoding.UTF8.GetChars(line[..headerEnd], buffer)]
			: Encoding.UTF8.GetString(line[..headerEnd]).AsSpan();

		var first = header.IndexOf('\t');
		var second = first + 1 + header[(first + 1)..].IndexOf('\t');
		var third = second + 1 + header[(second + 1)..].IndexOf('\t');

		const NumberStyles IdStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

		if (DateTime.TryParseExact(header[..first], TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdAt) &&
			int.TryParse(header[(first + 1)..second], IdStyle, CultureInfo.InvariantCulture, out _) &&
			int.TryParse(header[(second + 1)..third], IdStyle, CultureInfo.InvariantCulture, out var threadId))
		{
			ticks = createdAt.Ticks;
			isUiThread = threadId == 1;
			return true;
		}

		return false;
	}

	public static long ParseTimestamp(ReadOnlySpan<byte> record)
	{
		var tab = record.IndexOf((byte)'\t');
		Span<char> buffer = stackalloc char[64];
		ReadOnlySpan<char> field = tab <= buffer.Length
			? buffer[..Encoding.UTF8.GetChars(record[..tab], buffer)]
			: Encoding.UTF8.GetString(record[..tab]).AsSpan();

		DateTime.TryParseExact(field, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdAt);
		return createdAt.Ticks;
	}

	/// <summary>
	/// Parses `yyyy-MM-dd HH:mm:ss.fffff` (1 to 5 fraction digits) followed by a tab.
	/// Returns <see langword="false"/> for anything else, so the caller can fall back to <see cref="ParseTimestamp"/>.
	/// </summary>
	public static bool TryParseTimestampFast(ReadOnlySpan<byte> s, out long ticks)
	{
		ticks = 0;

		if (s.Length < 21 || s[4] != '-' || s[7] != '-' || s[10] != ' ' || s[13] != ':' || s[16] != ':' || s[19] != '.')
		{
			return false;
		}

		if (!TryDigits(s.Slice(0, 4), out var year) || !TryDigits(s.Slice(5, 2), out var month) ||
			!TryDigits(s.Slice(8, 2), out var day) || !TryDigits(s.Slice(11, 2), out var hour) ||
			!TryDigits(s.Slice(14, 2), out var minute) || !TryDigits(s.Slice(17, 2), out var second))
		{
			return false;
		}

		var position = 20;
		long fraction = 0;
		while (position < s.Length && position < 25 && (uint)(s[position] - '0') <= 9)
		{
			fraction = (fraction * 10) + (s[position] - '0');
			position++;
		}

		var digits = position - 20;
		if (digits == 0 || position >= s.Length || s[position] != '\t' ||
			year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) ||
			hour > 23 || minute > 59 || second > 59)
		{
			return false;
		}

		ticks = new DateTime(year, month, day, hour, minute, second).Ticks + (fraction * FractionScale[digits]);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryDigits(ReadOnlySpan<byte> s, out int value)
	{
		value = 0;
		foreach (var b in s)
		{
			var digit = (uint)(b - '0');
			if (digit > 9)
			{
				return false;
			}

			value = (value * 10) + (int)digit;
		}

		return true;
	}
}

/// <summary>
/// The log's bytes plus, for each record, where it starts, its line number, the UI-thread fact and the timestamp column.
/// </summary>
internal sealed class IndexedLog
{
	public required string Path { get; init; }
	public required byte[] Bytes { get; init; }
	public required int[] Starts { get; init; }
	public required int[] Lengths { get; init; }
	public required int[] LineNumbers { get; init; }
	public required bool[] IsUiThread { get; init; }
	public required long[] Ticks { get; init; }
	public required TimeSpan IndexingTime { get; init; }

	public int Count => this.Starts.Length;

	public long FileBytes => this.Bytes.LongLength;

	public static IndexedLog Load(string path)
	{
		if (new FileInfo(path).Length > Array.MaxLength)
		{
			throw new InvalidOperationException($"The log must be under {Array.MaxLength:N0} bytes. Path={path}");
		}

		Console.Error.WriteLine($"Reading {path}...");
		var bytes = File.ReadAllBytes(path);

		Console.Error.WriteLine("Indexing...");
		var stopwatch = Stopwatch.StartNew();

		var starts = new List<int>();
		var lengths = new List<int>();
		var lineNumbers = new List<int>();
		var isUiThread = new List<bool>();
		var ticks = new List<long>();

		ReadOnlySpan<byte> source = bytes;
		var position = source.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
		var lineNumber = 1;
		var recordStart = -1;

		while (position < source.Length)
		{
			var lineStart = position;
			var lineBreak = source[position..].IndexOfAny((byte)'\r', (byte)'\n');
			var lineEnd = lineBreak < 0 ? source.Length : position + lineBreak;

			if (TsvHeader.TryRead(source[lineStart..lineEnd], out var recordTicks, out var recordIsUiThread))
			{
				if (recordStart >= 0)
				{
					lengths.Add(LengthWithoutLineBreak(source, recordStart, lineStart));
				}

				recordStart = lineStart;
				starts.Add(lineStart);
				lineNumbers.Add(lineNumber);
				isUiThread.Add(recordIsUiThread);
				ticks.Add(recordTicks);
			}

			position = NextLine(source, lineEnd);
			lineNumber++;
		}

		if (recordStart >= 0)
		{
			lengths.Add(LengthWithoutLineBreak(source, recordStart, source.Length));
		}

		return new IndexedLog
		{
			Path = path,
			Bytes = bytes,
			Starts = starts.ToArray(),
			Lengths = lengths.ToArray(),
			LineNumbers = lineNumbers.ToArray(),
			IsUiThread = isUiThread.ToArray(),
			Ticks = ticks.ToArray(),
			IndexingTime = stopwatch.Elapsed,
		};
	}

	private static int LengthWithoutLineBreak(ReadOnlySpan<byte> source, int start, int end)
	{
		if (end > start && source[end - 1] == '\n')
		{
			end--;
		}

		if (end > start && source[end - 1] == '\r')
		{
			end--;
		}

		return end - start;
	}

	private static int NextLine(ReadOnlySpan<byte> source, int lineEnd)
	{
		if (lineEnd >= source.Length)
		{
			return lineEnd;
		}

		return source[lineEnd] == '\r' && lineEnd + 1 < source.Length && source[lineEnd + 1] == '\n'
			? lineEnd + 2
			: lineEnd + 1;
	}
}

/// <summary>
/// A repeatable Weevil TSV log: times that mostly move forward, occasional gaps of 1–10 s,
/// occasional out-of-order records, and a chosen share of records on the UI thread (thread 1).
/// </summary>
internal static class SyntheticLog
{
	private const int Version = 2;

	// Bytes in a record besides its words: timestamp, IDs, severity, tabs, "sequence=N" and the line break.
	private const double FixedRecordBytes = 62;

	private static readonly string[] Words =
	[
		"request", "accepted", "queue", "worker", "completed", "timeout", "retry", "connection",
		"sensor", "value", "updated", "cache", "miss", "session", "opened", "closed", "frame", "rendered",
	];

	private static readonly double AverageWordBytes = Words.Average(word => word.Length + 1);

	public static string EnsureExists(Arguments arguments)
	{
		var megabytes = arguments.SizeMegabytes;
		var folder = arguments.LogFolder ?? Path.GetTempPath();
		var path = Path.Combine(folder, $"Weevil.TimestampPoC.v{Version}.{megabytes}MB.{arguments.RecordBytes}B.{arguments.UiPercent}UI.log");
		var targetBytes = megabytes * 1_000_000L;

		if (File.Exists(path) && new FileInfo(path).Length > targetBytes - 10_000)
		{
			Console.Error.WriteLine($"Reusing {path}");
			return path;
		}

		Console.Error.WriteLine($"Generating a {megabytes:N0} MB log: {path}");
		Directory.CreateDirectory(folder);

		// Word counts are drawn evenly from 0 to twice the average, to vary record lengths.
		var averageWords = Math.Max(0, (arguments.RecordBytes - FixedRecordBytes) / AverageWordBytes);
		var maxWords = (int)Math.Round(averageWords * 2);

		var random = new Random(42);
		var time = new DateTime(2026, 10, 8, 10, 0, 0);
		var line = new StringBuilder(256);
		long written = 0;
		var sequence = 0;

		using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1 << 20);

		while (true)
		{
			// The format has 5 fraction digits: steps of 100 ticks (10 µs).
			time = time.AddTicks(random.Next(0, 2_000) * 100L);
			if (random.Next(5_000) == 0)
			{
				time = time.AddMilliseconds(random.Next(1_000, 10_000));
			}

			DateTime timestamp = random.Next(2_000) == 0 ? time.AddMilliseconds(-random.Next(1, 2_000)) : time;
			var threadId = random.Next(100) < arguments.UiPercent ? 1 : random.Next(2, 25);

			line.Clear();
			line.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture)).Append('\t')
				.Append(1000).Append('\t')
				.Append(threadId).Append('\t')
				.Append(PickSeverity(random)).Append('\t');

			var wordCount = random.Next(0, maxWords + 1);
			for (var word = 0; word < wordCount; word++)
			{
				line.Append(Words[random.Next(Words.Length)]).Append(' ');
			}

			line.Append("sequence=").Append(sequence++).Append("\r\n");

			// Every character is ASCII, so one byte each.
			if (written + line.Length > targetBytes)
			{
				break;
			}

			writer.Write(line);
			written += line.Length;
		}

		return path;
	}

	private static string PickSeverity(Random random) => random.Next(100) switch
	{
		< 70 => "Information",
		< 85 => "Debug",
		< 93 => "Warning",
		< 99 => "Error",
		_ => "Critical",
	};
}

internal sealed class Arguments
{
	public int SizeMegabytes { get; private set; } = 1_000;
	public int RecordBytes { get; private set; } = 131;
	public int UiPercent { get; private set; } = 20;
	public string? LogFolder { get; private set; }
	public string? FilePath { get; private set; }
	public int FilterPercent { get; private set; } = 10;
	public int Threads { get; private set; } = Environment.ProcessorCount;
	public IReadOnlyList<Way> Ways { get; private set; } = [Way.Column, Way.Plugin, Way.Span, Way.Fast];
	public int Runs { get; private set; } = 3;
	public string? ReportPath { get; private set; }
	public bool ShowHelp { get; private set; }

	public static Arguments Parse(string[] args)
	{
		var arguments = new Arguments();

		for (var i = 0; i < args.Length; i++)
		{
			var name = args[i];
			string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{name} needs a value.");
			int Number(int minimum, int maximum) => int.TryParse(Value(), out var number) && number >= minimum && number <= maximum
				? number
				: throw new ArgumentException($"{name} must be a number from {minimum} to {maximum}.");

			switch (name)
			{
				case "--size-mb":
					arguments.SizeMegabytes = Number(10, 2_000);
					break;
				case "--record-bytes":
					arguments.RecordBytes = Number(70, 4_000);
					break;
				case "--ui-percent":
					arguments.UiPercent = Number(0, 100);
					break;
				case "--log-folder":
					arguments.LogFolder = Value();
					break;
				case "--file":
					var file = Value();
					arguments.FilePath = File.Exists(file) ? file : throw new ArgumentException($"File not found: {file}");
					break;
				case "--filter-percent":
					arguments.FilterPercent = Number(1, 100);
					break;
				case "--threads":
					arguments.Threads = Number(1, 256);
					break;
				case "--ways":
					arguments.Ways = ParseWays(Value());
					break;
				case "--runs":
					arguments.Runs = Number(1, 20);
					break;
				case "--report":
					arguments.ReportPath = Value();
					break;
				case "--help":
				case "-h":
					arguments.ShowHelp = true;
					break;
				default:
					throw new ArgumentException($"Unknown option: {name}");
			}
		}

		return arguments;
	}

	/// <summary>
	/// The column always comes first: it is the baseline the other ways are compared with.
	/// </summary>
	private static List<Way> ParseWays(string value)
	{
		var ways = new List<Way> { Way.Column };

		foreach (var name in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			Way way = Enum.TryParse(name, ignoreCase: true, out Way parsed)
				? parsed
				: throw new ArgumentException($"Unknown way: {name}. Use column, plugin, span or fast.");

			if (!ways.Contains(way))
			{
				ways.Add(way);
			}
		}

		return ways;
	}

	public static void PrintHelp()
	{
		Console.WriteLine("Weevil timestamp proof of concept (#1008)");
		Console.WriteLine();
		Console.WriteLine("The log:");
		Console.WriteLine("  --size-mb <10-2000>        Size of the synthetic log, in MB. Default 1000.");
		Console.WriteLine("  --record-bytes <70-4000>   Average record size in the synthetic log. Default 131.");
		Console.WriteLine("  --ui-percent <0-100>       Share of synthetic records on the UI thread. Default 20.");
		Console.WriteLine("  --log-folder <path>        Where the synthetic log is written. Default: the temporary folder.");
		Console.WriteLine("  --file <path>              Use a real Weevil TSV log instead: UTF-8, under 2 GB.");
		Console.WriteLine();
		Console.WriteLine("The measurements:");
		Console.WriteLine("  --filter-percent <1-100>   Share of records the previous filter kept, for the @Elapsed filter. Default 10.");
		Console.WriteLine($"  --threads <1-256>          Threads for the parallel read. Default {Environment.ProcessorCount}, this computer's processors.");
		Console.WriteLine("  --ways <list>              Ways to compare, from column, plugin, span and fast. Default all.");
		Console.WriteLine("  --runs <1-20>              Timed runs per workload; the median is reported. Default 3.");
		Console.WriteLine("  --report <path>            Also save the report to this file.");
	}
}
