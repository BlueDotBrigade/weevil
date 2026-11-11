namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// The reference logs the benchmarks open, written once to a temporary folder and kept between runs.
	/// </summary>
	/// <remarks>
	/// BenchmarkDotNet runs each benchmark in its own process, so a log is written only when it is missing.
	/// The reference log is the same for a given record count, and Weevil saves no index or sidecar in the temporary folder,
	/// so each open starts from the same state.
	/// </remarks>
	internal static class BenchmarkLogs
	{
		private static readonly string Folder = Path.Combine(Path.GetTempPath(), "Weevil.Benchmarks");

		public static string GetPath(int recordCount)
		{
			var path = Path.Combine(Folder, $"ReferenceLog.{recordCount}.log");

			if (!File.Exists(path))
			{
				Directory.CreateDirectory(Folder);
				var written = ReferenceLog.Create(Folder, recordCount);

				if (!string.Equals(written, path, StringComparison.Ordinal))
				{
					File.Move(written, path, overwrite: true);
				}
			}

			return path;
		}

		/// <summary>
		/// Text that only the last record holds, so a search reads every record.
		/// </summary>
		public static string LastRecordText(int recordCount) => $"sequence={recordCount - 1}";
	}
}
