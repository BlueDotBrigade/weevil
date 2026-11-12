namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// The reference logs the benchmarks open, written once to a temporary folder and kept between runs.
	/// </summary>
	/// <remarks>
	/// BenchmarkDotNet runs each benchmark in its own process, so a log is written only when it is missing.
	/// It is written under another name and then moved into place, so a run that is stopped, or another run at the same time, never leaves or reads a partial log.
	/// The reference log is the same for a given record count. After a change to <c>ReferenceLog.cs</c>, delete the folder.
	/// Weevil saves no line index or sidecar in the temporary folder, so each open starts from the same state: without a saved line index.
	/// </remarks>
	internal static class BenchmarkLogs
	{
		private static readonly string Folder = Path.Combine(Path.GetTempPath(), "Weevil.Benchmarks");

		public static string GetPath(int recordCount)
		{
			var path = Path.Combine(Folder, $"ReferenceLog.{recordCount}.log");

			if (!File.Exists(path))
			{
				var writingFolder = Directory.CreateDirectory(Path.Combine(Folder, $"Writing.{Environment.ProcessId}")).FullName;

				try
				{
					var written = ReferenceLog.Create(writingFolder, recordCount);

					try
					{
						File.Move(written, path);
					}
					catch (IOException) when (File.Exists(path))
					{
						// Another run wrote the same log first.
					}
				}
				finally
				{
					Directory.Delete(writingFolder, recursive: true);
				}
			}

			return path;
		}

		/// <summary>
		/// Opens the reference log, and checks that it holds every record, so a benchmark never measures a smaller log than it says.
		/// </summary>
		/// <param name="storageMode">RAM, as the GUI opens a log today, or Disk (#1028).</param>
		public static IEngine Open(int recordCount, IO.StorageMode storageMode = IO.StorageMode.Ram)
		{
			IEngine engine = Engine.UsingPath(GetPath(recordCount)).UsingStorage(storageMode).Open();

			if (engine.Count != recordCount)
			{
				throw new InvalidOperationException($"The reference log holds {engine.Count:N0} records instead of {recordCount:N0}. Delete {Folder}, so it is written again.");
			}

			return engine;
		}

		/// <summary>
		/// A file name for this process alone, beside the reference logs, so two runs at the same time do not write the same file.
		/// </summary>
		public static string GetOutputPath(string name) => Path.Combine(Folder, $"{Path.GetFileNameWithoutExtension(name)}.{Environment.ProcessId}{Path.GetExtension(name)}");

		/// <summary>
		/// Text that only the last record holds, so a search reads every record.
		/// </summary>
		public static string LastRecordText(int recordCount) => $"sequence={recordCount - 1}";
	}
}
