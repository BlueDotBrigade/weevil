namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using BlueDotBrigade.Weevil.Data;
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
		/// Logged when a log is opened from the record table saved with its index, without being parsed (#1023, #1085).
		/// </summary>
		internal const string RecordsReadFromIndexMessage = "The records were read from the saved index, without parsing the log.";

		/// <summary>
		/// Returns the saved index when it matches the log, after scanning any content added since. Otherwise scans the whole log.
		/// </summary>
		/// <remarks>
		/// The index is saved whenever it changes, unless it cannot be: Weevil then continues without saving it.
		/// </remarks>
		public static LineIndex Load(string logFilePath, Stream logSource)
		{
			var indexFilePath = GetIndexFilePath(logFilePath);
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

		/// <summary>
		/// Reads the line count, and the length of the log it covers, from the saved index's header, without checking that the index matches the log.
		/// </summary>
		/// <remarks>
		/// For an estimate only (#1028). Returns <see langword="false"/> when there is no index, or it cannot be read.
		/// </remarks>
		public static bool TryReadLineCount(string logFilePath, out int lineCount, out long indexedLength)
		{
			lineCount = 0;
			indexedLength = 0;

			var indexFilePath = GetIndexFilePath(logFilePath);

			if (!System.IO.File.Exists(indexFilePath))
			{
				return false;
			}

			try
			{
				using var indexSource = new System.IO.FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

				return LineIndex.TryReadLineCount(indexSource, out lineCount, out indexedLength);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				return false;
			}
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

		private static void TrySave(LineIndex index, string logFilePath, string indexFilePath, RecordTable table = null)
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
					index.Write(destination, table);
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

		/// <summary>
		/// After a full parse, saves with the index what parsed the log, the checksums of the bytes it read, and the record table it made,
		/// unless the saved index has them already (#1023).
		/// </summary>
		/// <remarks>
		/// The line index is saved before the log is parsed, by <see cref="Load"/>, and again here once the parse is known.
		/// </remarks>
		public static void SaveParse(string logFilePath, LineIndex index, SavedParse parse, RecordTable table)
		{
			ArgumentNullException.ThrowIfNull(index);
			ArgumentNullException.ThrowIfNull(parse);
			ArgumentNullException.ThrowIfNull(table);

			if (parse.IsSameAs(index.Parse) && index.HasTable)
			{
				return;
			}

			index.Parse = parse;
			TrySave(index, logFilePath, GetIndexFilePath(logFilePath), table);
		}

		/// <summary>
		/// Builds the record table saved with the index, instead of parsing the log, when the same parser would make it and the log has not changed since (#1023).
		/// </summary>
		/// <remarks>
		/// Every block of the log is checked against the checksums saved with the table: one that changed means the log must be parsed.
		/// A log that grew since is parsed too.
		/// </remarks>
		/// <param name="source">On disk, the source the records read their content back through: otherwise <see langword="null"/>.</param>
		/// <returns><see langword="false"/> when the log must be parsed.</returns>
		public static bool TryLoadTable(string logFilePath, Stream logSource, LineIndex index, IPlugin plugin, StorageMode storageMode, out RecordTable table, out LogFileSource source)
		{
			table = null;
			source = null;

			SavedParse parse = index.Parse;

			if (parse is null || !index.HasTable || !parse.IsSameParser(plugin, storageMode) || parse.EndOffset != logSource.Length)
			{
				return false;
			}

			var indexFilePath = GetIndexFilePath(logFilePath);
			var stopwatch = Stopwatch.StartNew();
			var originalPosition = logSource.Position;

			try
			{
				using var indexSource = new System.IO.FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				indexSource.Position = index.TableOffset;

				if (!SavedTableReader.TryRead(indexSource, logSource, System.IO.Path.GetFullPath(logFilePath), parse, index.ContentEncoding, storageMode, out table, out source))
				{
					Log.Default.Write(
						LogSeverityType.Information,
						$"The saved record table does not match the log, so the log will be parsed. IndexFilePath={indexFilePath}");
					return false;
				}
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is FormatException || exception is ArgumentException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The saved record table could not be read, so the log will be parsed. IndexFilePath={indexFilePath}");
				table = null;
				source = null;
				return false;
			}
			finally
			{
				logSource.Position = originalPosition;
			}

			Log.Default.Write(
				LogSeverityType.Information,
				RecordsReadFromIndexMessage,
				new Dictionary<string, object>
				{
					{ "RecordCount", table.Count },
					{ "Duration", stopwatch.Elapsed.TotalSeconds },
					{ "IndexFilePath", indexFilePath },
				});

			return true;
		}

		/// <summary>
		/// Deletes the saved index, so the next open rebuilds it: it checks only the start and end of the log,
		/// so it can still match a log changed in the middle (#1025).
		/// </summary>
		public static void Delete(string logFilePath)
		{
			TryDelete(GetIndexFilePath(logFilePath));
		}

		private static string GetIndexFilePath(string logFilePath)
		{
			return $"{logFilePath}.{FileExtension}";
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
				Log.Default.Write(LogSeverityType.Warning, exception, $"A file could not be deleted. FilePath={filePath}");
			}
		}
	}
}
