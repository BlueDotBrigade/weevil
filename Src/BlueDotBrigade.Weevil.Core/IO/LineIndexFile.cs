namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using Diagnostics;

	/// <summary>
	/// Saves a log's <see cref="LineIndex"/> beside it, named like the sidecar: <c>Application.log.idx</c>.
	/// </summary>
	/// <remarks>
	/// The file is a cache: it is rebuilt when it is missing, corrupt, or does not match the log, so deleting it is always safe.
	/// Like the sidecar, it is not saved for a log from a compressed archive.
	/// </remarks>
	internal static class LineIndexFile
	{
		public const string FileExtension = "idx";

		/// <summary>
		/// Returns the saved index when it matches the log, after scanning any content added since. Otherwise scans the whole log.
		/// </summary>
		/// <remarks>
		/// The index is saved whenever it changes, unless it cannot be: Weevil then continues without saving it.
		/// </remarks>
		public static LineIndex Load(string logFilePath, Stream logSource)
		{
			var indexFilePath = $"{logFilePath}.{FileExtension}";
			var stopwatch = Stopwatch.StartNew();
			var hasChanged = true;

			if (TryRead(indexFilePath, logSource, out LineIndex index))
			{
				hasChanged = index.IndexedLength < logSource.Length;

				if (hasChanged)
				{
					index.Update(logSource);
				}
			}
			else
			{
				index = LineIndex.Build(logSource);
			}

			if (hasChanged)
			{
				TrySave(index, logFilePath, indexFilePath);
			}

			Log.Default.Write(
				LogSeverityType.Information,
				"The line index is ready.",
				new Dictionary<string, object>
				{
					{ "LineCount", index.LineCount },
					{ "IndexedLength", index.IndexedLength },
					{ "HasChanged", hasChanged },
					{ "Duration", stopwatch.Elapsed.TotalSeconds },
					{ "IndexFilePath", indexFilePath },
				});

			return index;
		}

		private static bool TryRead(string indexFilePath, Stream logSource, out LineIndex index)
		{
			index = null;

			if (!System.IO.File.Exists(indexFilePath))
			{
				return false;
			}

			try
			{
				using var indexSource = new System.IO.FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

				if (LineIndex.TryRead(indexSource, logSource, out index))
				{
					return true;
				}

				Log.Default.Write(
					LogSeverityType.Information,
					$"The saved line index does not match the log, so it will be rebuilt. IndexFilePath={indexFilePath}");
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The saved line index could not be read, so it will be rebuilt. IndexFilePath={indexFilePath}");
			}

			return false;
		}

		private static void TrySave(LineIndex index, string logFilePath, string indexFilePath)
		{
			if (FileHelper.IsInTemporaryDirectory(logFilePath))
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"Skipping line index save for file from compressed archive. IndexFilePath={indexFilePath}");
				return;
			}

			// Written to a temporary file first, so a failure never leaves a partial index.
			var temporaryFilePath = $"{indexFilePath}.tmp";

			try
			{
				using (var destination = new System.IO.FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					index.Write(destination);
				}

				System.IO.File.Move(temporaryFilePath, indexFilePath, overwrite: true);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The line index could not be saved, so it will be rebuilt the next time the log is opened. IndexFilePath={indexFilePath}");

				TryDelete(temporaryFilePath);
			}
		}

		private static void TryDelete(string filePath)
		{
			try
			{
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Warning, exception, $"A temporary file could not be deleted. FilePath={filePath}");
			}
		}
	}
}
