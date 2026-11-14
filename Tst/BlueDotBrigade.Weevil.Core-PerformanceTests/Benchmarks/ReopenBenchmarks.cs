namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// Reopening a log: building the record table saved with its index, instead of parsing the log (#1023).
	/// </summary>
	/// <remarks>
	/// Weevil saves no index in the temporary folder, so each reference log is copied to a folder of its own, outside it, once for each storage mode:
	/// the saved table is reused only in the mode that made it. Compare with <see cref="OpenBenchmarks"/>, which parses the log.
	/// </remarks>
	[MemoryDiagnoser]
	public class ReopenBenchmarks
	{
		// Logged by LineIndexFile.RecordsReadFromIndexMessage, which a Release build cannot see.
		private const string RecordsReadFromIndexMessage = "The records were read from the saved index, without parsing the log.";

		private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Weevil.Benchmarks");

		private string _logPath = string.Empty;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[Params(IO.StorageMode.Ram, IO.StorageMode.Disk)]
		public IO.StorageMode Storage { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_logPath = Path.Combine(Folder, $"ReferenceLog.{this.Records}.{this.Storage}.log");

			if (!File.Exists(_logPath))
			{
				Directory.CreateDirectory(Folder);
				var copyPath = $"{_logPath}.{Environment.ProcessId}";
				File.Copy(BenchmarkLogs.GetPath(this.Records), copyPath, overwrite: true);

				try
				{
					File.Move(copyPath, _logPath);
				}
				catch (IOException) when (File.Exists(_logPath))
				{
					// Another run copied the same log first.
					File.Delete(copyPath);
				}
			}

			// Once to save the index, if it is missing or stale, then once to check that a reopen reads it, so the benchmark never measures a parse.
			_ = Open();

			ILogWriter original = Log.Default;
			var messages = new MessageWriter(original);
			Log.Register(messages);

			try
			{
				if (Open() != this.Records || !messages.HasWritten(RecordsReadFromIndexMessage))
				{
					throw new InvalidOperationException($"The reopen did not read the saved index. Delete {Folder}, so the logs are copied again.");
				}
			}
			finally
			{
				Log.Register(original);
			}
		}

		[Benchmark]
		public int Open()
		{
			return Engine.UsingPath(_logPath).UsingStorage(this.Storage).Open().Count;
		}

		/// <summary>
		/// Notes each message written, and passes each entry on to the writer it replaces.
		/// </summary>
		private sealed class MessageWriter : ILogWriter
		{
			private readonly ILogWriter _next;
			private readonly HashSet<string> _messages = new HashSet<string>();

			public MessageWriter(ILogWriter next)
			{
				_next = next;
			}

			public bool HasWritten(string message) => _messages.Contains(message);

			public void Write(string message) => Write(LogSeverityType.Debug, message, null);

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(LogSeverityType.Debug, message, metadata);

			public void Write(LogSeverityType severity, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>>? metadata)
			{
				_messages.Add(message);
				_next.Write(severity, message, metadata ?? Array.Empty<KeyValuePair<string, object>>());
			}

			public void Write(LogSeverityType severity, Exception exception) => _next.Write(severity, exception);

			public void Write(LogSeverityType severity, Exception exception, string message) => _next.Write(severity, exception, message);

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) => _next.Write(severity, exception, message, metadata);
		}
	}
}
