// Times the same operations against Weevil 2.12.1 (NuGet, WEEVIL2) and against this repository's Core.
// Usage: OpsHarness <log> <runs> [ram|disk] [text of the last record, for Find]
// Build against main: dotnet build -c Release. Against 2.12.1: dotnet build -c Release -p:WeevilVersion=2.12.1
using System.Diagnostics;
using System.Globalization;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;
using BlueDotBrigade.Weevil.Navigation;

internal static class Program
{
	private static IEngine _engine = null!;
	private static int _runs;

	private static int Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		var path = args[0];
		_runs = int.Parse(args[1], CultureInfo.InvariantCulture);
		var mode = args.Length > 2 ? args[2] : "ram";
		var lastText = args.Length > 3 ? args[3] : "sequence=0";

		using var process = Process.GetCurrentProcess();
		var stopwatch = Stopwatch.StartNew();
#if WEEVIL2
		_engine = Engine.UsingPath(path).Open();
#else
		_engine = Engine.UsingPath(path)
			.UsingStorage(mode == "disk" ? BlueDotBrigade.Weevil.IO.StorageMode.Disk : BlueDotBrigade.Weevil.IO.StorageMode.Ram)
			.Open();
#endif
		var openSeconds = stopwatch.Elapsed.TotalSeconds;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		process.Refresh();
		Console.WriteLine($"OP\topen\t{openSeconds:F2}\t-\t{ResultCount()}\tws={process.WorkingSet64 / 1e9:F2}GB heap={GC.GetTotalMemory(false) / 1e9:F2}GB peakWhileOpening={PeakGb():F2}GB mode={mode}");
		ResetPeak();

		Filter("plain: timeout", FilterType.PlainText, "timeout", string.Empty);
		Filter("plain: sequence=123456 (rare)", FilterType.PlainText, "sequence=123456", string.Empty);
		Filter("regex: retry.*connection", FilterType.RegularExpression, "retry.*connection", string.Empty);
		Filter("regex: (timeout|retry)\\s+\\w+\\s+(closed|opened)", FilterType.RegularExpression, @"(timeout|retry)\s+\w+\s+(closed|opened)", string.Empty);
		Filter("regex include timeout, exclude retry", FilterType.RegularExpression, "timeout", "retry");
		Filter("moniker: @Severity=Warning", FilterType.RegularExpression, "@Severity=Warning", string.Empty);
		Filter("regex OR: Warning||Error", FilterType.RegularExpression, "Warning||Error", string.Empty);
		Filter("clear (show all)", FilterType.RegularExpression, string.Empty, string.Empty);

		Measure("find: last record's text", () =>
		{
			_engine.Navigate.GoTo(1, RecordSearchType.ExactMatch);
			return _engine.Navigate.NextContent(lastText, isCaseSensitive: true).LineNumber;
		});

		process.Refresh();
		GC.Collect();
		Console.WriteLine($"END\tws={process.WorkingSet64 / 1e9:F2}GB peak={process.PeakWorkingSet64 / 1e9:F2}GB heap={GC.GetTotalMemory(true) / 1e9:F2}GB");
		return 0;
	}

	// Linux: the process's peak resident set (VmHWM), which writing 5 to clear_refs resets.
	private static double PeakGb()
	{
		foreach (var line in File.ReadLines("/proc/self/status"))
		{
			if (line.StartsWith("VmHWM:", StringComparison.Ordinal))
			{
				return long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture) * 1024 / 1e9;
			}
		}

		return 0;
	}

	private static void ResetPeak() => File.WriteAllText("/proc/self/clear_refs", "5");

	private static int ResultCount()
	{
#if WEEVIL2
		return _engine.Filter.Results.Length;
#else
		return _engine.Filter.Results.Count;
#endif
	}

	private static void Filter(string name, FilterType type, string include, string exclude)
	{
		Measure(name, () =>
		{
			_engine.Filter.Apply(type, new FilterCriteria(include, exclude));
			return ResultCount();
		});
	}

	private static void Measure(string name, Func<int> operation)
	{
		var seconds = new List<double>();
		var allocated = new List<long>();
		var result = 0;
		try
		{
			for (var i = 0; i < _runs; i++)
			{
				var before = GC.GetTotalAllocatedBytes(precise: true);
				var stopwatch = Stopwatch.StartNew();
				result = operation();
				seconds.Add(stopwatch.Elapsed.TotalSeconds);
				allocated.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"OP\t{name}\tFAILED\t{e.GetType().Name}: {e.Message}");
			return;
		}

		seconds.Sort();
		allocated.Sort();
		Console.WriteLine($"OP\t{name}\t{seconds[seconds.Count / 2]:F3}\t{allocated[allocated.Count / 2] / 1e6:F1}\t{result}\tmin={seconds[0]:F3} max={seconds[^1]:F3} peakWs={PeakGb():F2}GB");
		ResetPeak();
	}
}
