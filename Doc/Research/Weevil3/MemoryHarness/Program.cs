// Memory harness for #978: how much RAM Weevil uses after opening a log.
// The same harness gives the "before" figures (a release from NuGet) and the "after" figures (this repository's Core).

namespace Weevil.MemoryHarness;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BlueDotBrigade.Weevil;

internal static class Program
{
	private const string MeasureOption = "--measure";
	private const string RamOption = "--ram";
	private const string ResultPrefix = "RESULT";
	private const double Gigabyte = 1_000_000_000;

	// Smaller logs are dominated by the fixed memory of the process, so they give no useful figure per GB.
	private const long SmallestLogToEstimateFrom = 100_000_000;

	// Common amounts of RAM in a PC, to say which one would hold a log.
	private static readonly int[] PcMemoryGigabytes = [16, 32, 64, 128];

	public static int Main(string[] args)
	{
		// Each log is opened in a fresh process, so one log's memory never affects the next.
		if (args.Length == 2 && args[0] == MeasureOption)
		{
			return MeasureThisProcess(args[1]);
		}

		if (args.Length == 1 && args[0] == RamOption)
		{
			// The memory load is only known after a garbage collection.
			GC.Collect();
			GCMemoryInfo memory = GC.GetGCMemoryInfo();
			Console.WriteLine(string.Join('\t',
				memory.TotalAvailableMemoryBytes.ToString(CultureInfo.InvariantCulture),
				Math.Max(0, memory.TotalAvailableMemoryBytes - memory.MemoryLoadBytes).ToString(CultureInfo.InvariantCulture)));
			return 0;
		}

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

		// The RAM .NET lets a measuring process use, which can be less than the machine's in a container, and how much of it is free.
		// The limit is a share of the free RAM, so a log that would make the computer page to disk is stopped first.
		// It is asked of a child, because the measuring processes run with different garbage collector settings than this one.
		var ram = RunChild(RamOption).Trim().Split('\t');
		var ramBytes = long.Parse(ram[0], CultureInfo.InvariantCulture);
		var freeBytes = long.Parse(ram[1], CultureInfo.InvariantCulture);
		var limitBytes = (long)(freeBytes * arguments.RamPercent / 100.0);

		var results = new List<Result>();
		foreach (var path in arguments.GetLogs())
		{
			// A saved line index would make this a reopen. Every synthetic log is measured as a first open.
			if (arguments.IsSynthetic(path))
			{
				DeleteLineIndex(path);
			}

			Console.Error.WriteLine($"Opening {path}...");

			try
			{
				Result result = MeasureInChildProcess(path, limitBytes);
				Console.Error.WriteLine(result.IsTooBig
					? $"  Too big: stopped at {Gb(result.PeakWorkingSetBytes)} GB after {result.OpenSeconds:N0} s."
					: $"  {result.Records:N0} records in {result.OpenSeconds:N1} s, working set {Gb(result.WorkingSetBytes)} GB, peak {Gb(result.PeakWorkingSetBytes)} GB.");
				results.Add(result);
			}
			catch (InvalidOperationException e)
			{
				// One failure does not lose the other logs' results.
				Console.Error.WriteLine($"  {e.Message}");
				results.Add(new Result(path, new FileInfo(path).Length, 0, 0, 0, 0, 0, 0, IsTooBig: false, Error: e.Message));
			}
			finally
			{
				if (arguments.DeleteLogs && arguments.IsSynthetic(path))
				{
					File.Delete(path);
					DeleteLineIndex(path);
				}
			}
		}

		var report = CreateReport(args, arguments, results, ramBytes, freeBytes, limitBytes);
		Console.WriteLine();
		Console.Write(report);

		if (arguments.ReportPath is not null)
		{
			File.WriteAllText(arguments.ReportPath, report);
			Console.Error.WriteLine($"Report saved: {Path.GetFullPath(arguments.ReportPath)}");
		}

		return 0;
	}

	private static int MeasureThisProcess(string path)
	{
		// Weevil is loaded first, so the memory before opening includes its assemblies and the empty log it opens at startup.
		RuntimeHelpers.RunClassConstructor(typeof(Engine).TypeHandle);

		using var process = Process.GetCurrentProcess();
		process.Refresh();
		var baselineBytes = process.WorkingSet64;

		var stopwatch = Stopwatch.StartNew();
		IEngine engine = Engine.UsingPath(path).Open();
		var openSeconds = stopwatch.Elapsed.TotalSeconds;

		process.Refresh();

		Console.WriteLine(string.Join('\t',
			ResultPrefix,
			new FileInfo(path).Length,
			engine.Count,
			openSeconds.ToString("R", CultureInfo.InvariantCulture),
			baselineBytes,
			GC.GetTotalMemory(forceFullCollection: false),
			process.WorkingSet64,
			process.PeakWorkingSet64));

		GC.KeepAlive(engine);
		return 0;
	}

	private static void DeleteLineIndex(string logPath)
	{
		File.Delete(logPath + ".idx");
		File.Delete(logPath + ".idx.tmp");
	}

	private static ProcessStartInfo CreateChild(params string[] args)
	{
		var self = Environment.ProcessPath ?? throw new InvalidOperationException("Unable to find this program's path.");
		var start = new ProcessStartInfo(self) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };

		// Under the dotnet host, the child needs the program's DLL.
		if (Path.GetFileNameWithoutExtension(self).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
		{
			start.ArgumentList.Add(typeof(Program).Assembly.Location);
		}

		foreach (var arg in args)
		{
			start.ArgumentList.Add(arg);
		}

		// In a container, .NET limits the managed heap to 75% of the container's memory, and throws OutOfMemoryException beyond it.
		// 99% (hexadecimal 63), the most .NET accepts, leaves the limit to this harness, so a log too big for the RAM is reported the same way everywhere.
		start.Environment["DOTNET_GCHeapHardLimitPercent"] = "0x63";

		return start;
	}

	private static string RunChild(params string[] args)
	{
		using Process child = Process.Start(CreateChild(args)) ?? throw new InvalidOperationException("Unable to start a child process.");
		Task<string> errors = child.StandardError.ReadToEndAsync();
		var output = child.StandardOutput.ReadToEnd();
		child.WaitForExit();
		return child.ExitCode == 0
			? output
			: throw new InvalidOperationException($"A child process failed, exit code {child.ExitCode}: {errors.Result}");
	}

	private static Result MeasureInChildProcess(string path, long limitBytes)
	{
		ProcessStartInfo start = CreateChild(MeasureOption, path);

		var stopwatch = Stopwatch.StartNew();
		using Process child = Process.Start(start) ?? throw new InvalidOperationException("Unable to start the measuring process.");
		Task<string> output = child.StandardOutput.ReadToEndAsync();
		Task<string> errors = child.StandardError.ReadToEndAsync();

		// A log too big for the RAM would make the computer page to disk, or the operating system end the process.
		// Instead, the child is stopped once its working set passes the limit, and the log is reported as too big.
		long highestBytes = 0;
		var isTooBig = false;
		while (!child.WaitForExit(100))
		{
			try
			{
				child.Refresh();
				highestBytes = Math.Max(highestBytes, child.WorkingSet64);
			}
			catch (InvalidOperationException)
			{
				// The child exited between the two calls.
			}

			if (highestBytes > limitBytes)
			{
				isTooBig = true;
				child.Kill(entireProcessTree: true);
				child.WaitForExit();
				break;
			}
		}

		var seconds = stopwatch.Elapsed.TotalSeconds;
		var logBytes = new FileInfo(path).Length;

		var line = output.Result
			.Split('\n', StringSplitOptions.TrimEntries)
			.FirstOrDefault(l => l.StartsWith(ResultPrefix + "\t", StringComparison.Ordinal));

		// The operating system ends a process that runs out of memory (exit code 137 on Linux),
		// or .NET throws OutOfMemoryException when the managed heap reaches its limit.
		var ranOutOfMemory = child.ExitCode == 137 || errors.Result.Contains("Out of memory", StringComparison.OrdinalIgnoreCase)
			|| errors.Result.Contains(nameof(OutOfMemoryException), StringComparison.Ordinal);

		if (isTooBig || (line is null && ranOutOfMemory))
		{
			return new Result(path, logBytes, Records: 0, seconds, 0, 0, 0, Math.Max(highestBytes, limitBytes), IsTooBig: true);
		}

		if (child.ExitCode != 0 || line is null)
		{
			throw new InvalidOperationException($"Measuring failed, exit code {child.ExitCode}: {path}{Environment.NewLine}{errors.Result.Trim()}");
		}

		var fields = line.Split('\t');
		return new Result(
			path,
			long.Parse(fields[1], CultureInfo.InvariantCulture),
			int.Parse(fields[2], CultureInfo.InvariantCulture),
			double.Parse(fields[3], CultureInfo.InvariantCulture),
			long.Parse(fields[4], CultureInfo.InvariantCulture),
			long.Parse(fields[5], CultureInfo.InvariantCulture),
			long.Parse(fields[6], CultureInfo.InvariantCulture),
			long.Parse(fields[7], CultureInfo.InvariantCulture),
			IsTooBig: false);
	}

	private static string CreateReport(string[] args, Arguments arguments, List<Result> results, long ramBytes, long freeBytes, long limitBytes)
	{
		var w = new StringWriter(CultureInfo.InvariantCulture);

		Assembly core = typeof(Engine).Assembly;
		var source = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == "WeevilSource").Value;
		var coreVersion = core.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? core.GetName().Version?.ToString();

		w.WriteLine("# Weevil memory after opening a log (#978)");
		w.WriteLine();
		w.WriteLine("| | |");
		w.WriteLine("|---|---|");
		w.WriteLine($"| Weevil | {source}: Core {coreVersion} |");
		w.WriteLine($"| Measured | {DateTime.Now:yyyy-MM-dd}, options `{(args.Length == 0 ? "(defaults)" : string.Join(' ', args))}` |");
		w.WriteLine($"| Machine | {Environment.ProcessorCount} logical processors, {Gb(ramBytes)} GB of RAM available to .NET, {Gb(freeBytes)} GB of it free when the run started, {RuntimeInformation.OSDescription} |");
		w.WriteLine($"| .NET | {RuntimeInformation.FrameworkDescription}, {(GCSettings.IsServerGC ? "server" : "workstation")} garbage collector |");

		w.WriteLine();
		w.WriteLine("## Measured");
		w.WriteLine();
		w.WriteLine("- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.");
		w.WriteLine("- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.");
		w.WriteLine("- **Peak:** the most it held while opening.");
		w.WriteLine("- **Managed heap:** the .NET objects in that working set.");
		w.WriteLine($"- **Too big:** opening was stopped when the working set passed {Gb(limitBytes)} GB: {arguments.RamPercent}% of the RAM free when the run started.");
		w.WriteLine();
		w.WriteLine("| Log | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |");
		w.WriteLine("|---|---|---|---|---|---|---|---|");
		foreach (Result r in results)
		{
			w.WriteLine(r.Error is not null
				? $"| {Size(r.LogBytes)} | | | **Failed:** {r.Error.ReplaceLineEndings(" ")} | | | | |"
				: r.IsTooBig
					? $"| {Size(r.LogBytes)} | | | stopped after {r.OpenSeconds:N0} s | | | | **Too big:** over {Gb(limitBytes)} GB |"
					: $"| {Size(r.LogBytes)} | {r.Records:N0} | {(r.Records == 0 ? string.Empty : ((double)r.LogBytes / r.Records).ToString("N0", CultureInfo.InvariantCulture))} | {r.OpenSeconds:N1} s | {Gb(r.BaselineBytes)} GB | {Gb(r.ManagedBytes)} GB | {Gb(r.WorkingSetBytes)} GB | {Gb(r.PeakWorkingSetBytes)} GB |");
		}

		var measured = results.Where(r => !r.IsTooBig && r.Error is null).ToList();
		Result? largest = measured.MaxBy(r => r.LogBytes);
		if (largest is null || largest.LogBytes < SmallestLogToEstimateFrom)
		{
			w.WriteLine();
			w.WriteLine($"Nothing is estimated: that needs a log of at least {Size(SmallestLogToEstimateFrom)} that fits in the RAM.");
			return w.ToString();
		}

		// Memory grows with the number of records, so the largest log gives the best figure per GB.
		// The memory before opening is the same for every log, so it is left out of the figure per GB.
		var baseline = largest.BaselineBytes / Gigabyte;
		var perGb = (largest.WorkingSetBytes - largest.BaselineBytes) / (double)largest.LogBytes;
		var peakPerGb = (largest.PeakWorkingSetBytes - largest.BaselineBytes) / (double)largest.LogBytes;

		w.WriteLine();
		w.WriteLine("## Per GB of Log");
		w.WriteLine();
		w.WriteLine($"From the {Size(largest.LogBytes)} log: **{perGb:N2} GB** of working set per GB of log after opening, and **{peakPerGb:N2} GB** at the peak while opening, on top of {baseline:N2} GB before opening.");

		// Logs too big to measure are estimated as well.
		var estimates = arguments.EstimateGigabytes
			.Select(gigabytes => (double)gigabytes)
			.Concat(results.Where(r => r.IsTooBig).Select(r => Math.Round(r.LogBytes / Gigabyte, 1)))
			.Distinct()
			.Order()
			.ToList();

		if (estimates.Count > 0)
		{
			w.WriteLine();
			w.WriteLine("## Estimated");
			w.WriteLine();
			w.WriteLine("**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.");
			w.WriteLine("The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.");
			w.WriteLine();
			w.WriteLine("| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |");
			w.WriteLine("|---|---|---|---|");
			foreach (var gigabytes in estimates)
			{
				var peak = baseline + (gigabytes * peakPerGb);
				var ram = PcMemoryGigabytes.FirstOrDefault(pc => peak <= pc);
				w.WriteLine($"| {gigabytes:0.#} GB | {baseline + (gigabytes * perGb):N1} GB | {peak:N1} GB | {(ram == 0 ? $"More than {PcMemoryGigabytes[^1]} GB" : $"{ram} GB")} |");
			}
		}

		return w.ToString();
	}

	private static string Gb(long bytes) => (bytes / Gigabyte).ToString("N1", CultureInfo.InvariantCulture);

	private static string Size(long bytes) => bytes < Gigabyte
		? (bytes / 1_000_000.0).ToString("N0", CultureInfo.InvariantCulture) + " MB"
		: Gb(bytes) + " GB";

	private sealed record Result(
		string Path,
		long LogBytes,
		int Records,
		double OpenSeconds,
		long BaselineBytes,
		long ManagedBytes,
		long WorkingSetBytes,
		long PeakWorkingSetBytes,
		bool IsTooBig,
		string? Error = null);
}

/// <summary>
/// A repeatable Weevil TSV log, made the same way as the timestamp proof of concept's (`../TimestampPoC`):
/// times that mostly move forward, occasional gaps and out-of-order records, and 20% of records on the UI thread.
/// </summary>
internal static class SyntheticLog
{
	private const int Version = 1;

	// Bytes in a record besides its words: timestamp, IDs, severity, tabs, "sequence=N" and the line break.
	private const double FixedRecordBytes = 62;

	private static readonly string[] Words =
	[
		"request", "accepted", "queue", "worker", "completed", "timeout", "retry", "connection",
		"sensor", "value", "updated", "cache", "miss", "session", "opened", "closed", "frame", "rendered",
	];

	private static readonly double AverageWordBytes = Words.Average(word => word.Length + 1);

	public static string GetPath(int megabytes, int recordBytes, string folder) =>
		Path.Combine(folder, $"Weevil.MemoryHarness.v{Version}.{megabytes}MB.{recordBytes}B.log");

	public static string EnsureExists(int megabytes, int recordBytes, string folder)
	{
		var path = GetPath(megabytes, recordBytes, folder);
		var targetBytes = megabytes * 1_000_000L;

		if (File.Exists(path) && new FileInfo(path).Length > targetBytes - 10_000)
		{
			Console.Error.WriteLine($"Reusing {path}");
			return path;
		}

		Console.Error.WriteLine($"Generating a {megabytes:N0} MB log: {path}");
		Directory.CreateDirectory(folder);

		// Word counts are drawn evenly from 0 to twice the average, to vary record lengths.
		var averageWords = Math.Max(0, (recordBytes - FixedRecordBytes) / AverageWordBytes);
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
			var threadId = random.Next(5) == 0 ? 1 : random.Next(2, 25);

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
	private readonly HashSet<string> _syntheticLogs = [];

	public List<int> SizesMegabytes { get; } = [];
	public List<string> Files { get; } = [];
	public List<int> EstimateGigabytes { get; private set; } = [12, 16, 20, 24];
	public int RecordBytes { get; private set; } = 131;
	public string LogFolder { get; private set; } = Path.GetTempPath();
	public int RamPercent { get; private set; } = 90;
	public bool DeleteLogs { get; private set; }
	public string? ReportPath { get; private set; }
	public bool ShowHelp { get; private set; }

	public bool IsSynthetic(string path) => _syntheticLogs.Contains(path);

	public IEnumerable<string> GetLogs()
	{
		List<int> sizes = this.SizesMegabytes.Count == 0 && this.Files.Count == 0 ? [1_000] : this.SizesMegabytes;

		// Each synthetic log is generated just before it is opened, so --delete-logs needs room for one log at a time.
		foreach (var megabytes in sizes)
		{
			var path = SyntheticLog.EnsureExists(megabytes, this.RecordBytes, this.LogFolder);
			_syntheticLogs.Add(path);
			yield return path;
		}

		foreach (var file in this.Files)
		{
			yield return file;
		}
	}

	public static Arguments Parse(string[] args)
	{
		var arguments = new Arguments();

		for (var i = 0; i < args.Length; i++)
		{
			var name = args[i];
			string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{name} needs a value.");

			switch (name)
			{
				case "--size-mb":
					arguments.SizesMegabytes.AddRange(ParseList(Value(), 10, 64_000, "--size-mb takes sizes from 10 to 64000, separated by commas."));
					break;
				case "--estimate-gb":
					arguments.EstimateGigabytes = ParseList(Value(), 1, 1_000, "--estimate-gb takes sizes from 1 to 1000, separated by commas.");
					break;
				case "--record-bytes":
					arguments.RecordBytes = int.TryParse(Value(), out var recordBytes) && recordBytes is >= 70 and <= 4_000
						? recordBytes
						: throw new ArgumentException("--record-bytes must be a number from 70 to 4000.");
					break;
				case "--log-folder":
					arguments.LogFolder = Value();
					break;
				case "--file":
					var file = Value();
					arguments.Files.Add(File.Exists(file) ? file : throw new ArgumentException($"File not found: {file}"));
					break;
				case "--ram-percent":
					arguments.RamPercent = int.TryParse(Value(), out var percent) && percent is >= 10 and <= 100
						? percent
						: throw new ArgumentException("--ram-percent must be a number from 10 to 100.");
					break;
				case "--delete-logs":
					arguments.DeleteLogs = true;
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

	private static List<int> ParseList(string value, int minimum, int maximum, string error) =>
		value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(item => int.TryParse(item, out var number) && number >= minimum && number <= maximum ? number : throw new ArgumentException(error))
			.ToList();

	public static void PrintHelp()
	{
		Console.WriteLine("Weevil memory harness (#978)");
		Console.WriteLine();
		Console.WriteLine("  --size-mb <list>          Synthetic logs to open, in MB, separated by commas. Default 1000.");
		Console.WriteLine("  --record-bytes <70-4000>  Average record size in the synthetic logs. Default 131.");
		Console.WriteLine("  --log-folder <path>       Where the synthetic logs are written. Default: the temporary folder.");
		Console.WriteLine("  --delete-logs             Delete each synthetic log after measuring it, to save disk space.");
		Console.WriteLine("  --file <path>             Open a real log instead; repeat for several.");
		Console.WriteLine("  --estimate-gb <list>      Log sizes to estimate, in GB, separated by commas. Default 12,16,20,24.");
		Console.WriteLine("  --ram-percent <10-100>    Stop opening a log once it uses this share of the RAM. Default 90.");
		Console.WriteLine("  --report <path>           Also save the report to this file.");
	}
}
