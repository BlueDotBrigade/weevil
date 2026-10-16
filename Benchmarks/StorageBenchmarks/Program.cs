// Exploratory storage benchmark for Weevil (.NET 9, x64).
// Put this project in <weevil-repo>/Benchmarks/StorageBenchmarks/.
// See README.md for usage, constraints and memory-measurement interpretation.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using BlueDotBrigade.Weevil.Data;

public enum StorageKind
{
    Text,
    Bytes,
    Weevil
}

internal static class Program
{
    private const string DefaultPattern = "ERROR_CODE_42";

    public static int Main(string[] args)
    {
        try
        {
            var options = Arguments.Parse(args);
            if (options.Help)
            {
                PrintHelp();
                return 0;
            }

            string path = options.FilePath ?? Path.Combine(
                Path.GetTempPath(), $"weevil-storage-benchmark-{options.SizeMb}mb.tsv");

            if (options.FilePath is null)
            {
                SyntheticLog.EnsureExists(path, checked((long)options.SizeMb * 1_000_000L));
            }
            else if (!File.Exists(path))
            {
                throw new FileNotFoundException("Input log file not found.", path);
            }

            Console.WriteLine($"File: {path} ({new FileInfo(path).Length:N0} bytes)");
            Console.WriteLine($"Regex: {options.Pattern}");
            Console.WriteLine("Input encoding: UTF-8; format: Weevil TSV (five tab-delimited fields)");

            // BDN starts separate worker processes; environment variables are inherited.
            Environment.SetEnvironmentVariable("WEEVIL_BENCH_FILE", Path.GetFullPath(path));
            Environment.SetEnvironmentVariable("WEEVIL_BENCH_REGEX", options.Pattern);

            if (options.Mode.Equals("bdn", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Benchmark is "load" or "both")
                    BenchmarkRunner.Run<LoadBenchmarks>();
                if (options.Benchmark is "filter" or "both")
                    BenchmarkRunner.Run<FilterBenchmarks>();
                return 0;
            }

            if (options.Case.Equals("all", StringComparison.OrdinalIgnoreCase))
                return ProfileInIsolatedProcesses(args);

            return Profile.Run(path, options.Pattern,
                Enum.Parse<StorageKind>(options.Case, ignoreCase: true), options.Filters);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    // Profile each mode in a fresh process to avoid a previous 2 GB string
    // influencing the next mode's working-set/private-byte readings.
    private static int ProfileInIsolatedProcesses(string[] originalArgs)
    {
        long? expectedRecords = null;
        long? expectedMatches = null;

        foreach (StorageKind kind in Enum.GetValues<StorageKind>())
        {
            string processPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot locate the current process executable.");
            var info = new ProcessStartInfo(processPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            if (Path.GetFileNameWithoutExtension(processPath)
                .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);

            foreach (string arg in originalArgs)
                info.ArgumentList.Add(arg);
            info.ArgumentList.Add("--case");
            info.ArgumentList.Add(kind.ToString());

            Console.WriteLine($"\n========== {kind} (isolated process) ==========");
            using Process child = Process.Start(info)
                ?? throw new InvalidOperationException("Unable to start benchmark child process.");
            string output = child.StandardOutput.ReadToEnd();
            child.WaitForExit();
            Console.Write(output);
            if (child.ExitCode != 0)
                return child.ExitCode;

            const string Marker = "RESULT ";
            string? result = Array.Find(output.Split('\n'), x => x.StartsWith(Marker, StringComparison.Ordinal));
            if (result is null)
                throw new InvalidOperationException($"No result line from {kind} process.");
            string[] fields = result.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long records = long.Parse(fields[1].Split('=')[1], CultureInfo.InvariantCulture);
            long matches = long.Parse(fields[2].Split('=')[1], CultureInfo.InvariantCulture);
            if (expectedRecords is not null && expectedRecords != records)
                throw new InvalidOperationException($"Record count mismatch: expected {expectedRecords}, {kind} produced {records}. Input may not be compatible with the TSV baseline.");
            if (expectedMatches is not null && expectedMatches != matches)
                throw new InvalidOperationException($"Regex match count mismatch: expected {expectedMatches}, {kind} produced {matches}. Check newline normalization/TSV parsing and the supplied regex.");
            expectedRecords = records;
            expectedMatches = matches;
        }
        Console.WriteLine("\nPASS: All three cases returned equal record and match counts.");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Weevil storage exploration (run in Release/x64)

            --mode profile|bdn     profile (default) measures retained memory, CPU time,
                                   elapsed time and allocations; bdn uses BenchmarkDotNet
            --case all|Text|Bytes|Weevil
                                   profile cases; all (default) runs isolated processes
            --bench load|filter|both
                                   BDN benchmark groups (default both)
            --file PATH           use existing UTF-8 Weevil TSV log file
            --size-mb N           synthetic file size in decimal MB (default 1000)
            --regex PATTERN       regex applied to each complete record
                                   (default ERROR_CODE_42)
            --filters N           number of consecutive filtering passes (default 5)
            --help                show this message

            Example smoke test: dotnet run -c Release -- --size-mb 10
            Example 1 GB run: dotnet run -c Release -- --mode profile --size-mb 1000
            Example BDN: dotnet run -c Release -- --mode bdn --bench filter --size-mb 1000
            """);
    }

    private sealed record Arguments(
        string Mode, string Case, string Benchmark, string? FilePath,
        int SizeMb, string Pattern, int Filters, bool Help)
    {
        public static Arguments Parse(string[] args)
        {
            string mode = "profile", @case = "all", benchmark = "both";
            string? file = null;
            int sizeMb = 1000, filters = 5;
            string pattern = DefaultPattern;
            bool help = false;

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (key is "--help" or "-h") { help = true; continue; }
                if (++i >= args.Length) throw new ArgumentException($"Missing value for {key}.");
                string value = args[i];
                switch (key)
                {
                    case "--mode": mode = value.ToLowerInvariant(); break;
                    case "--case": @case = value; break;
                    case "--bench": benchmark = value.ToLowerInvariant(); break;
                    case "--file": file = value; break;
                    case "--size-mb": sizeMb = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "--regex": pattern = value; break;
                    case "--filters": filters = int.Parse(value, CultureInfo.InvariantCulture); break;
                    default: throw new ArgumentException($"Unknown option {key}.");
                }
            }

            if (mode is not ("profile" or "bdn")) throw new ArgumentException("--mode must be profile or bdn.");
            if (benchmark is not ("load" or "filter" or "both")) throw new ArgumentException("--bench must be load, filter or both.");
            if (!@case.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                !Enum.TryParse<StorageKind>(@case, true, out _))
                throw new ArgumentException("--case must be all, Text, Bytes or Weevil.");
            if (sizeMb < 1 || filters < 1) throw new ArgumentException("--size-mb and --filters must be positive.");
            return new(mode, @case, benchmark, file, sizeMb, pattern, filters, help);
        }
    }
}

// Avoids a custom/stub implementation: this invokes the ACTUAL internal
// MultilineRecordParser + TsvRecordParser from BlueDotBrigade.Weevil.Core.
// The cached delegate avoids MethodInfo.Invoke on every record.
internal static class WeevilParserAdapter
{
    private static readonly Assembly Core = Assembly.Load("BlueDotBrigade.Weevil.Core");
    private static readonly Type MultilineType = Core.GetType(
        "BlueDotBrigade.Weevil.Data.MultilineRecordParser", throwOnError: true)!;
    private static readonly Type TsvType = Core.GetType(
        "BlueDotBrigade.Weevil.Data.TsvRecordParser", throwOnError: true)!;

    public static List<IRecord> Load(string path)
    {
        var records = new List<IRecord>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, options: FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, TextStorage.Utf8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024);

        object tsv = Activator.CreateInstance(TsvType, nonPublic: true)!;
        object parser = Activator.CreateInstance(MultilineType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args: [reader, tsv, 256, 1, false], // maximumLinesToSearch, firstRecordLineNumber, logging disabled
            culture: null)!;
        var method = MultilineType.GetMethod("GetNext", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException(MultilineType.FullName, "GetNext");
        var getNext = method.CreateDelegate<Func<IRecord>>(parser);

        while (true)
        {
            IRecord record = getNext();
            if (!Record.IsGenuine(record)) break;
            records.Add(record);
        }
        return records;
    }
}

internal readonly record struct RecordSlice(int Start, int Length, int LineNumber);

internal sealed class LoadedData
{
    public StorageKind Kind { get; init; }
    public string? Text { get; init; }
    public byte[]? Bytes { get; init; }
    public List<RecordSlice>? Index { get; init; }
    public List<IRecord>? Records { get; init; }
    public int MaxRecordLength { get; init; }

    public int RecordCount => Records?.Count ?? Index?.Count ?? 0;

    public int Filter(Regex regex)
    {
        int matches = 0;
        switch (Kind)
        {
            case StorageKind.Text:
            {
                string text = Text!;
                foreach (RecordSlice entry in Index!)
                    if (regex.IsMatch(text.AsSpan(entry.Start, entry.Length))) matches++;
                break;
            }
            case StorageKind.Bytes:
            {
                byte[] bytes = Bytes!;
                // Rent once per filtering pass. UTF-8 needs at most one UTF-16 code
                // unit per byte, so MaxRecordLength is sufficient capacity.
                char[] chars = ArrayPool<char>.Shared.Rent(Math.Max(1, MaxRecordLength));
                try
                {
                    foreach (RecordSlice entry in Index!)
                    {
                        int count = TextStorage.Utf8.GetChars(
                            bytes.AsSpan(entry.Start, entry.Length), chars.AsSpan());
                        if (regex.IsMatch(chars.AsSpan(0, count))) matches++;
                    }
                }
                finally { ArrayPool<char>.Shared.Return(chars); }
                break;
            }
            case StorageKind.Weevil:
                foreach (IRecord record in Records!)
                    if (regex.IsMatch(record.Content.AsSpan())) matches++;
                break;
        }
        return matches;
    }
}

internal static class TextStorage
{
    public static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public static LoadedData Load(string path, StorageKind kind) => kind switch
    {
        StorageKind.Text => FromText(path),
        StorageKind.Bytes => FromBytes(path),
        StorageKind.Weevil => new LoadedData { Kind = kind, Records = WeevilParserAdapter.Load(path) },
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static LoadedData FromText(string path)
    {
        // Reads and decodes the entire UTF-8 log into one UTF-16 string.
        string text = File.ReadAllText(path, Utf8);
        List<RecordSlice> index = BuildTextIndex(text);
        return new LoadedData { Kind = StorageKind.Text, Text = text, Index = index };
    }

    private static LoadedData FromBytes(string path)
    {
        // Keeps original UTF-8 bytes for the life of the dataset.
        byte[] bytes = File.ReadAllBytes(path);
        List<RecordSlice> index = BuildByteIndex(bytes, out int maxRecordLength);
        return new LoadedData { Kind = StorageKind.Bytes, Bytes = bytes,
            Index = index, MaxRecordLength = maxRecordLength };
    }

    // The indexes identify complete records by validating the five TSV fields
    // of the first physical line. This allows real multiline TSV records too.
    // It is intentionally a small experimental indexer, NOT the Weevil parser.
    private static List<RecordSlice> BuildTextIndex(string text)
    {
        var result = new List<RecordSlice>();
        ReadOnlySpan<char> source = text.AsSpan();
        int start = -1, startLine = 0, lineNumber = 1, pos = 0;

        while (pos < source.Length)
        {
            int lineStart = pos;
            int relativeEnd = source[pos..].IndexOfAny('\r', '\n');
            int lineEnd = relativeEnd < 0 ? source.Length : pos + relativeEnd;
            ReadOnlySpan<char> line = source[lineStart..lineEnd];
            if (IsTsvStart(line))
            {
                if (start >= 0)
                    result.Add(new RecordSlice(start, TrimEnd(source, start, lineStart), startLine));
                start = lineStart;
                startLine = lineNumber;
            }
            pos = NextLine(source, lineEnd);
            lineNumber++;
        }
        if (start >= 0)
            result.Add(new RecordSlice(start, TrimEnd(source, start, source.Length), startLine));
        return result;
    }

    private static List<RecordSlice> BuildByteIndex(byte[] bytes, out int maxRecordLength)
    {
        var result = new List<RecordSlice>();
        ReadOnlySpan<byte> source = bytes.AsSpan();
        int pos = source.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
        int start = -1, startLine = 0, lineNumber = 1;
        maxRecordLength = 0;

        while (pos < source.Length)
        {
            int lineStart = pos;
            int relativeEnd = source[pos..].IndexOfAny((byte)'\r', (byte)'\n');
            int lineEnd = relativeEnd < 0 ? source.Length : pos + relativeEnd;
            if (IsTsvStart(source[lineStart..lineEnd]))
            {
                if (start >= 0)
                {
                    int length = TrimEnd(source, start, lineStart);
                    result.Add(new RecordSlice(start, length, startLine));
                    maxRecordLength = Math.Max(maxRecordLength, length);
                }
                start = lineStart;
                startLine = lineNumber;
            }
            pos = NextLine(source, lineEnd);
            lineNumber++;
        }
        if (start >= 0)
        {
            int length = TrimEnd(source, start, source.Length);
            result.Add(new RecordSlice(start, length, startLine));
            maxRecordLength = Math.Max(maxRecordLength, length);
        }
        return result;
    }

    private static int TrimEnd(ReadOnlySpan<char> source, int start, int end)
    {
        if (end > start && source[end - 1] == '\n') end--;
        if (end > start && source[end - 1] == '\r') end--;
        return end - start;
    }
    private static int TrimEnd(ReadOnlySpan<byte> source, int start, int end)
    {
        if (end > start && source[end - 1] == (byte)'\n') end--;
        if (end > start && source[end - 1] == (byte)'\r') end--;
        return end - start;
    }
    private static int NextLine(ReadOnlySpan<char> source, int end)
    {
        if (end >= source.Length) return end;
        if (source[end] == '\r' && end + 1 < source.Length && source[end + 1] == '\n') return end + 2;
        return end + 1;
    }
    private static int NextLine(ReadOnlySpan<byte> source, int end)
    {
        if (end >= source.Length) return end;
        if (source[end] == (byte)'\r' && end + 1 < source.Length && source[end + 1] == (byte)'\n') return end + 2;
        return end + 1;
    }

    private static bool IsTsvStart(ReadOnlySpan<byte> line)
    {
        // Decode ONLY the TSV header, not the record body, while indexing bytes.
        int headerEnd = 0;
        for (int i = 0; i < 4; i++)
        {
            int tab = line[headerEnd..].IndexOf((byte)'\t');
            if (tab < 0) return false;
            headerEnd += tab + 1;
        }
        if (headerEnd <= 256)
        {
            Span<char> prefix = stackalloc char[256];
            int chars = Utf8.GetChars(line[..headerEnd], prefix);
            return IsTsvStart(prefix[..chars]);
        }
        // Rare fallback for unusually long TSV fields.
        return IsTsvStart(Utf8.GetString(line[..headerEnd]).AsSpan());
    }

    private static bool IsTsvStart(ReadOnlySpan<char> line)
    {
        // Mirrors TsvRecordParser's record-start decision (valid timestamp,
        // two integer fields, fourth tab, and a fifth content field).
        if (line.Trim().IsEmpty) return false;
        int first = line.IndexOf('\t');
        if (first < 0) return false;
        int secondRel = line[(first + 1)..].IndexOf('\t');
        if (secondRel < 0) return false;
        int second = first + 1 + secondRel;
        int thirdRel = line[(second + 1)..].IndexOf('\t');
        if (thirdRel < 0) return false;
        int third = second + 1 + thirdRel;
        int fourthRel = line[(third + 1)..].IndexOf('\t');
        if (fourthRel < 0) return false;

        return DateTime.TryParseExact(line[..first], "yyyy-MM-dd HH:mm:ss.FFFFF",
                   CultureInfo.InvariantCulture, DateTimeStyles.None, out _) &&
               int.TryParse(line[(first + 1)..second],
                   NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                   CultureInfo.InvariantCulture, out _) &&
               int.TryParse(line[(second + 1)..third],
                   NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                   CultureInfo.InvariantCulture, out _);
    }
}

internal static class SyntheticLog
{
    public static void EnsureExists(string path, long requestedBytes)
    {
        if (File.Exists(path) && new FileInfo(path).Length >= requestedBytes - 256 &&
            new FileInfo(path).Length <= requestedBytes)
        {
            Console.WriteLine("Reusing existing synthetic log file.");
            return;
        }

        Console.WriteLine($"Generating ~{requestedBytes:N0} bytes of single-line UTF-8 TSV logs...");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        byte[][] lines = new byte[16][];
        for (int i = 0; i < lines.Length; i++)
        {
            string message = i == 0 ? "ERROR_CODE_42 sample fault" : "Routine operation completed";
            lines[i] = TextStorage.Utf8.GetBytes(
                $"2026-10-08 10:00:00.12345\t1000\t1\tInformation\t" +
                $"{message}; source=engine; sequence={i:D2}; correlation=0123456789abcdef\n");
        }
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 1024 * 1024, options: FileOptions.SequentialScan);
        int lineIndex = 0;
        long total = 0;
        while (total + lines[lineIndex].Length <= requestedBytes)
        {
            ReadOnlySpan<byte> line = lines[lineIndex];
            file.Write(line);
            total += line.Length;
            lineIndex = (lineIndex + 1) % lines.Length;
        }
        Console.WriteLine($"Created {total:N0} bytes.");
    }
}

internal static class Profile
{
    public static int Run(string path, string pattern, StorageKind kind, int filters)
    {
        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(30));
        Collect();
        using Process process = Process.GetCurrentProcess();
        var baseline = Snapshot.Get(process);

        var clock = Stopwatch.StartNew();
        LoadedData data = TextStorage.Load(path, kind);
        clock.Stop();
        TimeSpan loadElapsed = clock.Elapsed;
        var postLoad = Snapshot.Get(process);
        Collect();
        var retained = Snapshot.Get(process);

        long matches = 0;
        clock.Restart();
        var beforeFilter = Snapshot.Get(process);
        for (int i = 0; i < filters; i++)
        {
            int count = data.Filter(regex);
            if (i == 0) matches = count;
            else if (count != matches)
                throw new InvalidOperationException("Different results across consecutive filters.");
        }
        clock.Stop();
        var afterFilter = Snapshot.Get(process);

        Console.WriteLine($"Records: {data.RecordCount:N0}");
        Console.WriteLine($"Load + index/parse: {loadElapsed.TotalSeconds:N3} s");
        Console.WriteLine($"Load CPU: {(postLoad.Cpu - baseline.Cpu).TotalSeconds:N3} s");
        Console.WriteLine($"Load allocated: {Mb(postLoad.Allocated - baseline.Allocated):N1} MB");
        Console.WriteLine($"Retained managed heap after load (full GC): {Mb(retained.ManagedHeap):N1} MB");
        Console.WriteLine($"Private bytes after load (full GC): {Mb(retained.PrivateBytes):N1} MB");
        Console.WriteLine($"Private-byte change from baseline: {Mb(retained.PrivateBytes - baseline.PrivateBytes):N1} MB");
        Console.WriteLine($"Peak working set up through load: {Mb(retained.PeakWorkingSet):N1} MB");
        Console.WriteLine($"Filtering {filters} times: {clock.Elapsed.TotalSeconds:N3} s");
        Console.WriteLine($"Filter CPU: {(afterFilter.Cpu - beforeFilter.Cpu).TotalSeconds:N3} s");
        Console.WriteLine($"Filter allocated: {Mb(afterFilter.Allocated - beforeFilter.Allocated):N1} MB");
        Console.WriteLine($"Match count per pass: {matches:N0}");
        Console.WriteLine($"RESULT records={data.RecordCount} matches={matches}");
        GC.KeepAlive(data);
        return 0;
    }

    private static double Mb(long bytes) => bytes / 1_000_000.0;
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private readonly record struct Snapshot(
        TimeSpan Cpu, long Allocated, long ManagedHeap, long PrivateBytes, long PeakWorkingSet)
    {
        public static Snapshot Get(Process process)
        {
            process.Refresh();
            return new Snapshot(process.TotalProcessorTime, GC.GetTotalAllocatedBytes(precise: true),
                GC.GetTotalMemory(forceFullCollection: false), process.PrivateMemorySize64,
                process.PeakWorkingSet64);
        }
    }
}

// BenchmarkDotNet measures time, *allocated* bytes and GCs. The separate
// --mode profile output measures retained heap and process CPU time.
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 0, iterationCount: 3)]
[InvocationCount(1, unrollFactor: 1)]
public class LoadBenchmarks
{
    [Params(StorageKind.Text, StorageKind.Bytes, StorageKind.Weevil)]
    public StorageKind Storage { get; set; }

    [Benchmark]
    public int LoadAndIndexOrParse()
    {
        LoadedData data = TextStorage.Load(BenchmarkEnvironment.Path, Storage);
        int count = data.RecordCount;
        GC.KeepAlive(data);
        return count;
    }
}

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 0, iterationCount: 3)]
[InvocationCount(1, unrollFactor: 1)]
public class FilterBenchmarks
{
    [Params(StorageKind.Text, StorageKind.Bytes, StorageKind.Weevil)]
    public StorageKind Storage { get; set; }

    private LoadedData _data = null!;
    private Regex _regex = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = TextStorage.Load(BenchmarkEnvironment.Path, Storage);
        _regex = new Regex(BenchmarkEnvironment.Pattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(30));
    }

    [Benchmark]
    public int FilterOnce() => _data.Filter(_regex);

    [Benchmark]
    public int FilterFiveTimes()
    {
        int total = 0;
        for (int i = 0; i < 5; i++) total += _data.Filter(_regex);
        return total;
    }
}

internal static class BenchmarkEnvironment
{
    public static string Path => Environment.GetEnvironmentVariable("WEEVIL_BENCH_FILE")
        ?? throw new InvalidOperationException("WEEVIL_BENCH_FILE is missing.");
    public static string Pattern => Environment.GetEnvironmentVariable("WEEVIL_BENCH_REGEX")
        ?? "ERROR_CODE_42";
}
