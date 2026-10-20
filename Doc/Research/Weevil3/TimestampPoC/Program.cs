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
	private const double TwentyGigabytes = 20_000_000_000;
	private const double SixtyFourGigabytes = 64_000_000_000;

	// Decision rule from the #988 plan, step 2.
	private const double MaxExtraSecondsAtOpen = 5;
	private const double MaxElapsedFilterSeconds = 10;

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

		var path = arguments.FilePath ?? SyntheticLog.EnsureExists(arguments.SizeMegabytes);
		var log = IndexedLog.Load(path);

		if (log.Count < 100)
		{
			Console.Error.WriteLine($"Too few records to measure: {log.Count}. Is this a Weevil TSV log?");
			return 1;
		}

		var inputs = Inputs.Create(log);

		VariantResult[] variants =
		[
			Measure("Column", new ColumnSource(log), log, inputs, arguments.Runs),
			Measure("Plugin parse", new PluginParseSource(log), log, inputs, arguments.Runs),
			Measure("Span parse", new SpanParseSource(log), log, inputs, arguments.Runs),
			Measure("Fast parse", new FastParseSource(log), log, inputs, arguments.Runs),
		];

		Report.Write(arguments, log, variants);
		return variants.All(v => v.HasSameAnswersAs(variants[0])) ? 0 : 1;
	}

	private static VariantResult Measure<T>(string name, T source, IndexedLog log, Inputs inputs, int runs)
		where T : struct, ITimestampSource
	{
		Console.Error.WriteLine($"Measuring: {name}...");

		return new VariantResult(name,
		[
			Measure(() => Workloads.Insights(source, log), runs),
			Measure(() => Workloads.ElapsedFilter(source, inputs.TenPercent), runs),
			Measure(() => Workloads.ElapsedFilter(source, inputs.AllRecords), runs),
			Measure(() => Workloads.GoToTimestamp(source, inputs.Targets, log.Count), runs),
			Measure(() => Workloads.RowsOnScreen(source, inputs.ScreenPositions), runs),
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

	internal static double ScaleTo(IndexedLog log, double bytes) => bytes / log.FileBytes;

	internal static class Report
	{
		private static readonly string[] WorkloadNames =
		[
			"Insights at open",
			"`@Elapsed` filter, 10% of records",
			"`@Elapsed` filter, all records",
			"Go to timestamp, ×1,000",
			"Rows on screen, ×10,000 screens",
		];

		public static void Write(Arguments arguments, IndexedLog log, VariantResult[] variants)
		{
			var scale = ScaleTo(log, TwentyGigabytes);
			var uiShare = (double)log.IsUiThread.Count(x => x) / log.Count;

			Console.WriteLine();
			Console.WriteLine("# Timestamp proof of concept (#1008)");
			Console.WriteLine();
			Console.WriteLine("| | |");
			Console.WriteLine("|---|---|");
			Console.WriteLine($"| Log | {(arguments.FilePath is null ? "Synthetic" : "Real")}, {log.FileBytes / 1_000_000.0:N0} MB |");
			Console.WriteLine($"| Records | {log.Count:N0}, averaging {(double)log.FileBytes / log.Count:N0} bytes |");
			Console.WriteLine($"| UI-thread records | {uiShare:P1} |");
			Console.WriteLine($"| Indexing, which also fills the column | {log.IndexingTime.TotalSeconds:N1} s |");
			Console.WriteLine($"| Machine | {Environment.ProcessorCount} logical processors, {RuntimeInformation.OSDescription}, {RuntimeInformation.FrameworkDescription} |");
			Console.WriteLine($"| Timing | Single thread, median of {arguments.Runs} {(arguments.Runs == 1 ? "run" : "runs")} |");

			Console.WriteLine();
			Console.WriteLine("## Measured (ms)");
			WriteTable(variants, [0, 1, 2, 3, 4], m => m.Milliseconds.ToString("N0", CultureInfo.InvariantCulture));

			Console.WriteLine();
			Console.WriteLine("## Scaled to a 20 GB log (s)");
			Console.WriteLine();
			Console.WriteLine("Only the workloads that read every record grow with the log.");
			WriteTable(variants, [0, 1, 2], m => (m.Milliseconds * scale / 1000).ToString("N1", CultureInfo.InvariantCulture));

			Console.WriteLine();
			Console.WriteLine("## Per action (µs)");
			Console.WriteLine();
			Console.WriteLine("These hardly grow with the log: a search in a 20 GB log takes about 10 more steps.");
			Console.WriteLine();
			Console.WriteLine($"| Action | {string.Join(" | ", variants.Select(v => v.Name))} |");
			Console.WriteLine($"|---|{string.Concat(variants.Select(_ => "---|"))}");
			Console.WriteLine($"| Go to timestamp | {string.Join(" | ", variants.Select(v => (v.Measurements[3].Milliseconds * 1000 / Inputs.TargetCount).ToString("N1", CultureInfo.InvariantCulture)))} |");
			Console.WriteLine($"| Elapsed times for one screen of 50 rows | {string.Join(" | ", variants.Select(v => (v.Measurements[4].Milliseconds * 1000 / Inputs.ScreenCount).ToString("N1", CultureInfo.InvariantCulture)))} |");

			Console.WriteLine();
			Console.WriteLine("## Memory for the column");
			Console.WriteLine();
			Console.WriteLine("| Log | Records | Column |");
			Console.WriteLine("|---|---|---|");
			Console.WriteLine($"| This log | {log.Count:N0} | {log.Count * 8 / 1_000_000.0:N0} MB |");
			Console.WriteLine($"| 20 GB | {log.Count * scale:N0} | {log.Count * scale * 8 / 1_000_000_000:N1} GB |");
			Console.WriteLine($"| 64 GB | {log.Count * ScaleTo(log, SixtyFourGigabytes):N0} | {log.Count * ScaleTo(log, SixtyFourGigabytes) * 8 / 1_000_000_000:N1} GB |");

			Console.WriteLine();
			Console.WriteLine("## Decision rule");
			Console.WriteLine();
			Console.WriteLine($"Keep the column, unless parsing on demand adds under {MaxExtraSecondsAtOpen} s to opening a 20 GB log and keeps an `@Elapsed` filter under {MaxElapsedFilterSeconds} s.");
			Console.WriteLine();
			Console.WriteLine("| Way | Extra at open | `@Elapsed` filter, all records | Within the rule? |");
			Console.WriteLine("|---|---|---|---|");

			string? fastEnough = null;
			foreach (VariantResult variant in variants.Skip(1))
			{
				var extraAtOpen = (variant.Measurements[0].Milliseconds - variants[0].Measurements[0].Milliseconds) * scale / 1000;
				var elapsedFilter = variant.Measurements[2].Milliseconds * scale / 1000;
				var isWithinRule = extraAtOpen < MaxExtraSecondsAtOpen && elapsedFilter < MaxElapsedFilterSeconds;
				fastEnough ??= isWithinRule ? variant.Name : null;

				Console.WriteLine($"| {variant.Name} | +{extraAtOpen:N1} s | {elapsedFilter:N1} s | {(isWithinRule ? "Yes" : "No")} |");
			}

			Console.WriteLine();
			Console.WriteLine(fastEnough is null
				? "**Answer:** keep the column. No way of parsing on demand is within the rule."
				: $"**Answer:** parsing on demand is fast enough with \"{fastEnough}\". The column can go.");

			var mismatches = variants.Skip(1).Where(v => !v.HasSameAnswersAs(variants[0])).Select(v => v.Name).ToList();
			Console.WriteLine();
			Console.WriteLine(mismatches.Count == 0
				? "All ways gave the same answers."
				: $"**Warning:** different answers from: {string.Join(", ", mismatches)}. The timings are not comparable.");
		}

		private static void WriteTable(VariantResult[] variants, int[] workloads, Func<Measurement, string> format)
		{
			Console.WriteLine();
			Console.WriteLine($"| Workload | {string.Join(" | ", variants.Select(v => v.Name))} |");
			Console.WriteLine($"|---|{string.Concat(variants.Select(_ => "---|"))}");

			foreach (var i in workloads)
			{
				Console.WriteLine($"| {WorkloadNames[i]} | {string.Join(" | ", variants.Select(v => format(v.Measurements[i])))} |");
			}
		}
	}
}

internal sealed record Measurement(object Answer, double Milliseconds);

internal sealed record VariantResult(string Name, Measurement[] Measurements)
{
	public bool HasSameAnswersAs(VariantResult other) =>
		this.Measurements.Zip(other.Measurements).All(pair => pair.First.Answer.Equals(pair.Second.Answer));
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
}

internal sealed class Inputs
{
	public const int TargetCount = 1_000;
	public const int ScreenCount = 10_000;

	public required int[] AllRecords { get; init; }
	public required int[] TenPercent { get; init; }
	public required long[] Targets { get; init; }
	public required int[] ScreenPositions { get; init; }

	public static Inputs Create(IndexedLog log)
	{
		var sample = new Random(7);
		var targets = new Random(11);
		var screens = new Random(13);

		var first = log.Ticks.Min();
		var last = log.Ticks.Max();

		return new Inputs
		{
			AllRecords = Enumerable.Range(0, log.Count).ToArray(),
			TenPercent = Enumerable.Range(0, log.Count).Where(_ => sample.Next(10) == 0).ToArray(),
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
/// occasional out-of-order records, and about 20% of records on the UI thread (thread 1).
/// </summary>
internal static class SyntheticLog
{
	private const int Version = 1;

	private static readonly string[] Words =
	[
		"request", "accepted", "queue", "worker", "completed", "timeout", "retry", "connection",
		"sensor", "value", "updated", "cache", "miss", "session", "opened", "closed", "frame", "rendered",
	];

	public static string EnsureExists(int megabytes)
	{
		var path = Path.Combine(Path.GetTempPath(), $"Weevil.TimestampPoC.v{Version}.{megabytes}MB.log");
		var targetBytes = megabytes * 1_000_000L;

		if (File.Exists(path) && new FileInfo(path).Length > targetBytes - 1_000)
		{
			Console.Error.WriteLine($"Reusing {path}");
			return path;
		}

		Console.Error.WriteLine($"Generating a {megabytes:N0} MB log: {path}");

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
			var threadId = random.Next(5) == 0 ? 1 : random.Next(2, 25);

			line.Clear();
			line.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture)).Append('\t')
				.Append(1000).Append('\t')
				.Append(threadId).Append('\t')
				.Append(PickSeverity(random)).Append('\t');

			var wordCount = random.Next(6, 16);
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
	public string? FilePath { get; private set; }
	public int Runs { get; private set; } = 3;
	public bool ShowHelp { get; private set; }

	public static Arguments Parse(string[] args)
	{
		var arguments = new Arguments();

		for (var i = 0; i < args.Length; i++)
		{
			string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

			switch (args[i])
			{
				case "--size-mb":
					arguments.SizeMegabytes = int.TryParse(Value(), out var size) && size is >= 10 and <= 2_000
						? size
						: throw new ArgumentException("--size-mb must be between 10 and 2000.");
					break;
				case "--file":
					var file = Value();
					arguments.FilePath = File.Exists(file) ? file : throw new ArgumentException($"File not found: {file}");
					break;
				case "--runs":
					arguments.Runs = int.TryParse(Value(), out var runs) && runs is >= 1 and <= 20
						? runs
						: throw new ArgumentException("--runs must be between 1 and 20.");
					break;
				case "--help":
				case "-h":
					arguments.ShowHelp = true;
					break;
				default:
					throw new ArgumentException($"Unknown argument: {args[i]}");
			}
		}

		return arguments;
	}

	public static void PrintHelp()
	{
		Console.WriteLine("Weevil timestamp proof of concept (#1008)");
		Console.WriteLine();
		Console.WriteLine("  --size-mb <10-2000>  Size of the synthetic log, in MB (default 1000).");
		Console.WriteLine("  --file <path>        Use a real Weevil TSV log (UTF-8, under 2 GB) instead.");
		Console.WriteLine("  --runs <1-20>        Timed runs per workload; the median is reported (default 3).");
	}
}
