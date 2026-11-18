using System.Diagnostics;
using System.Runtime.InteropServices;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;
using BlueDotBrigade.Weevil.IO;

// Times filters on one log, in RAM or on disk, with a warm or cold cache, and reports the median time and the bytes each filter allocates.
// Each filter runs once to warm up, then --iterations times. See README.md.
var options = Options.Parse(args);

var logPath = options.LogPath ?? WriteReferenceLog(options.ReferenceRecords, options.LogFolder);

var opening = Stopwatch.StartNew();
IEngine engine = Engine.UsingPath(logPath).UsingStorage(options.Storage).Open();
opening.Stop();

Console.WriteLine($"# {Path.GetFileName(logPath)}: {engine.Count:N0} records opened in {options.Storage} in {opening.Elapsed.TotalSeconds:F1} s, on {Environment.ProcessorCount} cores.");
Console.WriteLine("Log\tStorage\tCache\tFilter\tMedianMs\tMinMs\tMaxMs\tAllocatedMB\tResults");

Time("PlainText", FilterType.PlainText, options.PlainText);
Time("RegularExpression", FilterType.RegularExpression, options.RegularExpression);

void Time(string name, FilterType filterType, string expression)
{
	var criteria = new FilterCriteria(expression);

	// The first filter compiles the code and sizes the buffers, which the filters timed then reuse.
	engine.Filter.Apply(filterType, criteria);

	var milliseconds = new List<double>();
	var megabytes = new List<double>();

	for (var iteration = 0; iteration < options.Iterations; iteration++)
	{
		if (options.IsCold)
		{
			ColdCache.Evict(logPath);
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
		var stopwatch = Stopwatch.StartNew();
		engine.Filter.Apply(filterType, criteria);
		stopwatch.Stop();

		megabytes.Add((GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore) / 1_048_576.0);
		milliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
	}

	milliseconds.Sort();

	Console.WriteLine(string.Join('\t',
		Path.GetFileName(logPath),
		options.Storage,
		options.IsCold ? "Cold" : "Warm",
		name,
		$"{Median(milliseconds):F0}",
		$"{milliseconds[0]:F0}",
		$"{milliseconds[^1]:F0}",
		$"{Median(megabytes):F0}",
		engine.Filter.Results.Count));
}

static double Median(List<double> values)
{
	var sorted = values.Order().ToList();
	var middle = sorted.Count / 2;

	return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}

// Weevil saves no line index or sidecar in the temporary folder, so each run opens the reference log from the same state.
static string WriteReferenceLog(int recordCount, string folder)
{
	var path = Path.Combine(folder, $"ReferenceLog.{recordCount}.log");

	if (!System.IO.File.Exists(path))
	{
		System.IO.Directory.CreateDirectory(folder);
		System.IO.File.Move(ReferenceLog.Create(folder, recordCount), path);
	}

	return path;
}

internal sealed class Options
{
	public string LogPath { get; private set; }

	public int ReferenceRecords { get; private set; } = 1_000_000;

	public string LogFolder { get; private set; } = Path.Combine(Path.GetTempPath(), "Weevil.FilterTiming");

	public StorageMode Storage { get; private set; } = StorageMode.Ram;

	public int Iterations { get; private set; } = 7;

	public bool IsCold { get; private set; }

	public string PlainText { get; private set; } = "timeout";

	public string RegularExpression { get; private set; } = "retry.*connection";

	public static Options Parse(string[] args)
	{
		var options = new Options();

		for (var index = 0; index < args.Length; index++)
		{
			string Value() => index + 1 < args.Length ? args[++index] : throw new ArgumentException($"{args[index]} needs a value.");

			switch (args[index])
			{
				case "--log": options.LogPath = Value(); break;
				case "--reference": options.ReferenceRecords = int.Parse(Value()); break;
				case "--log-folder": options.LogFolder = Value(); break;
				case "--storage": options.Storage = Enum.Parse<StorageMode>(Value(), ignoreCase: true); break;
				case "--iterations": options.Iterations = int.Parse(Value()); break;
				case "--cold": options.IsCold = true; break;
				case "--plain": options.PlainText = Value(); break;
				case "--regex": options.RegularExpression = Value(); break;
				default: throw new ArgumentException($"Unknown option: {args[index]}. See README.md.");
			}
		}

		if (options.Iterations < 1)
		{
			throw new ArgumentException("--iterations must be at least 1.");
		}

		// Checked before the log is opened, which takes minutes for a large log.
		if (options.IsCold && !OperatingSystem.IsLinux())
		{
			throw new PlatformNotSupportedException("--cold needs Linux. On Windows, empty the standby list with Sysinternals RAMMap, as an administrator, before each run instead.");
		}

		if (options.IsCold && options.Storage != StorageMode.Disk)
		{
			throw new ArgumentException("--cold needs --storage Disk: in RAM, a filter never reads the log.");
		}

		return options;
	}
}

/// <summary>
/// Drops a file's pages from the operating system's cache, so the next read comes from the disk. Linux only: <see cref="Options.Parse"/> checks it, because Windows has no call for it that needs no administrator.
/// </summary>
internal static class ColdCache
{
	private const int DontNeed = 4; // POSIX_FADV_DONTNEED

	public static void Evict(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		var fileDescriptor = (int)stream.SafeFileHandle.DangerousGetHandle();

		// Pages not yet written to the disk stay in the cache, as they do for a reference log just written: write them first.
		if (fsync(fileDescriptor) != 0)
		{
			throw new IOException($"fsync failed with error {Marshal.GetLastPInvokeError()}.");
		}

		var error = posix_fadvise(fileDescriptor, 0, 0, DontNeed);

		if (error != 0)
		{
			throw new IOException($"posix_fadvise failed with error {error}.");
		}
	}

	[DllImport("libc", SetLastError = true)]
	private static extern int fsync(int fileDescriptor);

	[DllImport("libc")]
	private static extern int posix_fadvise(int fileDescriptor, long offset, long length, int advice);
}
