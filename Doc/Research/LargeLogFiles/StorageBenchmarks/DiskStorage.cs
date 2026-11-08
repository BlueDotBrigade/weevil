// Disk case of the storage benchmark (#1024).
// Like Weevil's disk mode, it keeps only where each record starts, and every filter reads the log's bytes
// from the drive, in parallel chunks that end at a record start. See README.md, "Disk case".

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BlueDotBrigade.Weevil.IO;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Options of the disk case. <see cref="Cache"/> is cold, warm or evict; <see cref="Text"/>, when set, replaces the regular expression with a plain-text filter.
/// </summary>
internal sealed record DiskOptions(int[] Threads, int ChunkKb, string Cache, string? Text, int Screens, int ScreenRows)
{
	public bool IsCold => this.Cache == "cold";

	public bool IsEvictOnly => this.Cache == "evict";
}

/// <summary>
/// Decides whether one record's text matches the filter.
/// </summary>
internal delegate bool Matcher(ReadOnlySpan<char> text);

internal static class DiskStorage
{
	private const int RandomSeed = 1024;

	public static int Run(string path, string pattern, int filters, DiskOptions options)
	{
		if (options.IsEvictOnly)
		{
			// Lets the owner check, for example with RAMMap's File Summary, that emptying the cache works.
			Console.WriteLine($"Cache: {Evict(path)}. Nothing else was measured.");
			return 0;
		}

		if (options.IsCold)
		{
			// Fails now, rather than after reading the whole log, when emptying the cache is not allowed.
			Evict(path);
		}

		var clock = Stopwatch.StartNew();
		var log = DiskLog.Open(path, checked(options.ChunkKb * 1024));
		clock.Stop();

		Console.WriteLine($"Records: {log.RecordCount:N0}");
		Console.WriteLine($"Open: {clock.Elapsed.TotalSeconds:N1} s to find where records start (not part of any filter time); {log.RecordCount * 8L / 1_000_000.0:N1} MB to keep them");
		Console.WriteLine($"Chunks: {log.ChunkCount:N0} of about {options.ChunkKb:N0} KB; largest chunk {log.MaxChunkBytes:N0} bytes; largest record {log.MaxRecordBytes:N0} bytes");
		Console.WriteLine(options.Text is null ? $"Filter: regular expression {pattern}" : $"Filter: plain text {options.Text}");
		Console.WriteLine($"Cache: {(options.IsCold ? "cold, emptied before each pass" : "warm")}; logical processors: {Environment.ProcessorCount}");

		long? expectedMatches = null;
		using var process = Process.GetCurrentProcess();

		foreach (var threads in options.Threads)
		{
			var passSeconds = new double[filters];

			for (var pass = 1; pass <= filters; pass++)
			{
				var cache = options.IsCold ? Evict(path) : "warm";

				process.Refresh();
				TimeSpan cpuBefore = process.TotalProcessorTime;
				clock.Restart();
				var matches = log.Filter(threads, CreateMatcher(pattern, options.Text));
				clock.Stop();
				process.Refresh();

				if (expectedMatches is not null && matches != expectedMatches)
				{
					throw new InvalidOperationException($"{threads} threads found {matches:N0} matches, but earlier passes found {expectedMatches:N0}.");
				}

				expectedMatches = matches;
				var seconds = clock.Elapsed.TotalSeconds;
				passSeconds[pass - 1] = seconds;
				var cpu = (process.TotalProcessorTime - cpuBefore).TotalSeconds;
				Console.WriteLine($"DISK threads={threads,2} pass={pass} seconds={seconds,8:N3} MB/s={log.Length / seconds / 1_000_000.0,7:N0} cpu={cpu,8:N3} matches={matches:N0} cache={cache}");
			}

			Array.Sort(passSeconds);
			Console.WriteLine($"SUMMARY threads={threads,2} median={passSeconds[passSeconds.Length / 2],8:N3} s min={passSeconds[0]:N3} s max={passSeconds[^1]:N3} s");
		}

		MeasureScreens(path, options);

		process.Refresh();
		Console.WriteLine($"Peak working set: {process.PeakWorkingSet64 / 1_000_000.0:N0} MB; private bytes: {process.PrivateMemorySize64 / 1_000_000.0:N0} MB");
		Console.WriteLine($"RESULT records={log.RecordCount} matches={expectedMatches}");
		return 0;
	}

	/// <summary>
	/// Removes the log from the operating system's file cache, so the next read comes from the drive.
	/// </summary>
	public static string Evict(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			// Opening the log unbuffered, while no other handle has it open, makes the file system purge its cached pages,
			// including pages still mapped in the system cache. This needs no administrator rights.
			File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, NoBuffering).Dispose();
			StandbyList.Purge();
			return "file cache emptied";
		}

		if (OperatingSystem.IsLinux())
		{
			using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			var result = PosixFadvise((int)handle.DangerousGetHandle(), 0, 0, PosixFadviseDontNeed);

			if (result != 0)
			{
				throw new InvalidOperationException($"posix_fadvise failed with error {result}.");
			}

			return "posix_fadvise";
		}

		throw new PlatformNotSupportedException("--cache cold and evict work on Windows and Linux only.");
	}

	private static Func<Matcher> CreateMatcher(string pattern, string? text)
	{
		if (text is not null)
		{
			// Weevil's case-sensitive plain-text filter.
			return () => line => line.Contains(text, StringComparison.Ordinal);
		}

		// One Regex per reader and pass, as Weevil's RegularExpression keeps one per thread for each filter:
		// a shared Regex caches one runner, so the other threads allocate one on every call.
		return () =>
		{
			var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(30));
			return line => regex.IsMatch(line);
		};
	}

	/// <summary>
	/// Reads screens of rows at random lines through Weevil's line index, which keeps the offset of every 64th line.
	/// </summary>
	private static void MeasureScreens(string path, DiskOptions options)
	{
		if (options.Screens < 1)
		{
			return;
		}

		var clock = Stopwatch.StartNew();
		LineIndex index;

		using (var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1, FileOptions.SequentialScan))
		{
			index = LineIndex.Build(source);
		}

		Console.WriteLine($"Line index: {index.LineCount:N0} lines, built in {clock.Elapsed.TotalSeconds:N1} s");

		var cache = options.IsCold ? Evict(path) : "warm";
		var random = new Random(RandomSeed);
		var milliseconds = new double[options.Screens];
		var buffer = new byte[64 * 1024];
		var chars = new char[buffer.Length];

		using (var log = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1, FileOptions.RandomAccess))
		{
			for (var screen = 0; screen < options.Screens; screen++)
			{
				var firstLine = random.Next(1, Math.Max(2, index.LineCount - options.ScreenRows + 2));
				var started = Stopwatch.GetTimestamp();
				ReadRows(log, index.GetByteOffset(log, firstLine), options.ScreenRows, buffer, chars);
				milliseconds[screen] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
			}
		}

		Array.Sort(milliseconds);
		var p95 = milliseconds[Math.Min(milliseconds.Length - 1, (int)(milliseconds.Length * 0.95))];
		Console.WriteLine($"SCREEN rows={options.ScreenRows} screens={options.Screens} median={milliseconds[milliseconds.Length / 2]:N3} ms p95={p95:N3} ms max={milliseconds[^1]:N3} ms cache={cache}");
	}

	/// <summary>
	/// Reads and decodes the given number of rows, starting at a byte offset.
	/// </summary>
	private static void ReadRows(FileStream log, long offset, int rows, byte[] buffer, char[] chars)
	{
		log.Position = offset;
		var found = 0;

		while (found < rows)
		{
			var read = log.Read(buffer, 0, buffer.Length);

			if (read == 0)
			{
				return;
			}

			var used = 0;

			while (found < rows && used < read)
			{
				var lineBreak = buffer.AsSpan(used, read - used).IndexOf((byte)'\n');
				var end = lineBreak < 0 ? read : used + lineBreak;

				// A row that crosses a read is decoded in two parts. Rows end at \n, as in the synthetic logs.
				Encoding.UTF8.GetChars(buffer.AsSpan(used, end - used), chars);

				if (lineBreak < 0)
				{
					break;
				}

				found++;
				used = end + 1;
			}
		}
	}

	private const int PosixFadviseDontNeed = 4;

	// FILE_FLAG_NO_BUFFERING, which .NET passes on to CreateFile on Windows.
	private const FileOptions NoBuffering = (FileOptions)0x20000000;

	[DllImport("libc", EntryPoint = "posix_fadvise")]
	private static extern int PosixFadvise(int fileDescriptor, long offset, long length, int advice);
}

/// <summary>
/// Where each record of a log starts, and the chunks a filter reads: about the requested size, each ending at a record start.
/// </summary>
internal sealed class DiskLog
{
	private readonly string _path;
	private readonly long[] _starts;
	private readonly int[] _chunks;
	private readonly long _length;

	private DiskLog(string path, long[] starts, int[] chunks, long length, int maxChunkBytes, int maxRecordBytes)
	{
		_path = path;
		_starts = starts;
		_chunks = chunks;
		_length = length;
		this.MaxChunkBytes = maxChunkBytes;
		this.MaxRecordBytes = maxRecordBytes;
	}

	public int RecordCount => _starts.Length;

	public int ChunkCount => _chunks.Length - 1;

	public long Length => _length;

	public int MaxChunkBytes { get; }

	public int MaxRecordBytes { get; }

	/// <summary>
	/// Reads the log once, as opening it would, and keeps only where each record starts.
	/// </summary>
	public static DiskLog Open(string path, int chunkBytes)
	{
		var starts = new List<long>();
		long length;

		using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 0, FileOptions.SequentialScan))
		{
			length = stream.Length;
			var buffer = new byte[4 * 1024 * 1024];
			var filled = 0;
			long bufferOffset = 0;
			var isFirstRead = true;

			while (true)
			{
				var read = stream.Read(buffer, filled, buffer.Length - filled);
				filled += read;
				var isEnd = read == 0;
				var position = 0;

				if (isFirstRead && filled >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				{
					position = 3;
				}

				isFirstRead = false;

				while (position < filled)
				{
					var relative = buffer.AsSpan(position, filled - position).IndexOfAny((byte)'\r', (byte)'\n');

					// An incomplete line, or a \r whose \n may follow: carry it into the next read.
					if ((relative < 0 || (buffer[position + relative] == (byte)'\r' && position + relative + 1 == filled)) && !isEnd)
					{
						break;
					}

					var lineEnd = relative < 0 ? filled : position + relative;

					if (TextStorage.IsTsvStart(buffer.AsSpan(position, lineEnd - position)))
					{
						starts.Add(bufferOffset + position);
					}

					if (lineEnd >= filled)
					{
						position = filled;
						break;
					}

					var isCrLf = buffer[lineEnd] == (byte)'\r' && lineEnd + 1 < filled && buffer[lineEnd + 1] == (byte)'\n';
					position = lineEnd + (isCrLf ? 2 : 1);
				}

				if (isEnd)
				{
					break;
				}

				var carried = filled - position;

				if (carried == buffer.Length)
				{
					Array.Resize(ref buffer, buffer.Length * 2);
				}

				Array.Copy(buffer, position, buffer, 0, carried);
				bufferOffset += position;
				filled = carried;
			}
		}

		return Split(path, starts.ToArray(), length, chunkBytes);
	}

	/// <summary>
	/// Reads every chunk once and returns how many records match.
	/// Each of the given number of readers runs on a thread of its own and reads a contiguous range of chunks.
	/// </summary>
	public long Filter(int threads, Func<Matcher> createMatcher)
	{
		var readers = Math.Max(1, Math.Min(threads, this.ChunkCount));
		var tasks = new Task<long>[readers];

		for (var reader = 0; reader < readers; reader++)
		{
			var firstChunk = (int)((long)this.ChunkCount * reader / readers);
			var lastChunk = (int)((long)this.ChunkCount * (reader + 1) / readers);
			tasks[reader] = Task.Factory.StartNew(
				() => FilterRange(firstChunk, lastChunk, createMatcher()),
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);
		}

		Task.WaitAll(tasks);
		long matches = 0;

		foreach (Task<long> task in tasks)
		{
			matches += task.Result;
		}

		return matches;
	}

	private static DiskLog Split(string path, long[] starts, long length, int chunkBytes)
	{
		var chunks = new List<int>();
		var maxChunkBytes = 0L;
		var maxRecordBytes = 0L;

		if (starts.Length > 0)
		{
			chunks.Add(0);
			var chunkStart = starts[0];

			for (var record = 0; record < starts.Length; record++)
			{
				var end = record + 1 < starts.Length ? starts[record + 1] : length;
				maxRecordBytes = Math.Max(maxRecordBytes, end - starts[record]);

				if (end - chunkStart >= chunkBytes || record + 1 == starts.Length)
				{
					chunks.Add(record + 1);
					maxChunkBytes = Math.Max(maxChunkBytes, end - chunkStart);
					chunkStart = end;
				}
			}
		}
		else
		{
			chunks.Add(0);
		}

		return new DiskLog(path, starts, chunks.ToArray(), length, checked((int)maxChunkBytes), checked((int)maxRecordBytes));
	}

	private long FilterRange(int firstChunk, int lastChunk, Matcher matcher)
	{
		// Each reader opens the log itself. On Windows, the reads of a handle opened for synchronous I/O are serialized,
		// and read-ahead is kept per handle, so readers sharing one handle would read one at a time.
		// Opened as disk mode will open it: the writer is never blocked, and the log can still be deleted.
		using SafeFileHandle handle = File.OpenHandle(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.SequentialScan);
		using var worker = new Worker(this.MaxChunkBytes, this.MaxRecordBytes, matcher);

		for (var chunk = firstChunk; chunk < lastChunk; chunk++)
		{
			FilterChunk(handle, chunk, worker);
		}

		return worker.Matches;
	}

	private void FilterChunk(SafeFileHandle handle, int chunk, Worker worker)
	{
		var first = _chunks[chunk];
		var last = _chunks[chunk + 1];
		var chunkStart = _starts[first];
		var chunkLength = (int)(End(last - 1) - chunkStart);
		ReadExactly(handle, worker.Bytes.AsSpan(0, chunkLength), chunkStart);

		for (var record = first; record < last; record++)
		{
			var start = (int)(_starts[record] - chunkStart);
			var end = (int)(End(record) - chunkStart);
			ReadOnlySpan<byte> content = TrimLineBreak(worker.Bytes.AsSpan(start, end - start));
			var count = TextStorage.Utf8.GetChars(content, worker.Chars);

			if (worker.IsMatch(worker.Chars.AsSpan(0, count)))
			{
				worker.Matches++;
			}
		}
	}

	private long End(int record)
	{
		return record + 1 < _starts.Length ? _starts[record + 1] : _length;
	}

	private static ReadOnlySpan<byte> TrimLineBreak(ReadOnlySpan<byte> record)
	{
		if (record.Length > 0 && record[^1] == (byte)'\n')
		{
			record = record[..^1];
		}

		if (record.Length > 0 && record[^1] == (byte)'\r')
		{
			record = record[..^1];
		}

		return record;
	}

	private static void ReadExactly(SafeFileHandle handle, Span<byte> buffer, long offset)
	{
		while (!buffer.IsEmpty)
		{
			var read = RandomAccess.Read(handle, buffer, offset);

			if (read == 0)
			{
				throw new EndOfStreamException($"The log ended at {offset:N0} bytes, before the chunk did.");
			}

			buffer = buffer[read..];
			offset += read;
		}
	}

	/// <summary>
	/// What one filter thread keeps: buffers for a chunk and a record, and its own matcher.
	/// </summary>
	private sealed class Worker : IDisposable
	{
		public Worker(int chunkBytes, int recordBytes, Matcher matcher)
		{
			this.Bytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, chunkBytes));

			// A UTF-8 record never decodes to more characters than it has bytes.
			this.Chars = ArrayPool<char>.Shared.Rent(Math.Max(1, recordBytes));
			this.IsMatch = matcher;
		}

		public byte[] Bytes { get; }

		public char[] Chars { get; }

		public Matcher IsMatch { get; }

		public long Matches { get; set; }

		public void Dispose()
		{
			ArrayPool<byte>.Shared.Return(this.Bytes);
			ArrayPool<char>.Shared.Return(this.Chars);
		}
	}
}

/// <summary>
/// Empties Windows' file cache, so files read earlier are no longer cached. Needs an elevated process.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StandbyList
{
	private const int SystemMemoryListInformation = 80;
	private const int MemoryFlushModifiedList = 3;
	private const int MemoryPurgeStandbyList = 4;
	private const uint TokenAdjustPrivileges = 0x20;
	private const uint TokenQuery = 0x8;
	private const uint PrivilegeEnabled = 0x2;
	private const int ErrorNotAllAssigned = 1300;

	public static void Purge()
	{
		EnablePrivilege("SeIncreaseQuotaPrivilege");
		EnablePrivilege("SeProfileSingleProcessPrivilege");

		// Trims the system cache's working set, so its pages join the standby and modified lists.
		// Unlike emptying every working set, this leaves the benchmark's own memory alone.
		if (!SetSystemFileCacheSize(UIntPtr.MaxValue, UIntPtr.MaxValue, 0))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// Writes pending changes, so recently written pages join the standby list, then empties it.
		SetMemoryList(MemoryFlushModifiedList);
		SetMemoryList(MemoryPurgeStandbyList);
	}

	private static void EnablePrivilege(string name)
	{
		if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var token))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		try
		{
			if (!LookupPrivilegeValue(null, name, out Luid luid))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			var privileges = new TokenPrivileges { PrivilegeCount = 1, Luid = luid, Attributes = PrivilegeEnabled };
			var isAdjusted = AdjustTokenPrivileges(token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero);
			var error = Marshal.GetLastWin32Error();

			if (!isAdjusted || error == ErrorNotAllAssigned)
			{
				throw new InvalidOperationException(
					"--cache cold and evict empty the file cache, which needs administrator rights: run the benchmark from an elevated prompt.");
			}
		}
		finally
		{
			CloseHandle(token);
		}
	}

	private static void SetMemoryList(int command)
	{
		var status = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));

		if (status != 0)
		{
			throw new InvalidOperationException($"Emptying the file cache failed: NtSetSystemInformation({command}) returned 0x{status:X8}.");
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Luid
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct TokenPrivileges
	{
		public uint PrivilegeCount;
		public Luid Luid;
		public uint Attributes;
	}

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

	[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool LookupPrivilegeValue(string? systemName, string name, out Luid luid);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetCurrentProcess();

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetSystemFileCacheSize(UIntPtr minimumFileCacheSize, UIntPtr maximumFileCacheSize, int flags);

	[DllImport("ntdll.dll")]
	private static extern int NtSetSystemInformation(int systemInformationClass, ref int systemInformation, int systemInformationLength);
}
