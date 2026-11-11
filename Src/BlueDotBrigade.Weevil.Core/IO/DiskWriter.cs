namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Threading;
	using Analysis;
	using Data;
	using Diagnostics;

	/// <summary>
	/// Produces a report that includes the amount of time that has elapsed between the selected log entries.
	/// </summary>
	/// <see href="https://www.jeremyshanks.com/fastest-way-to-write-text-files-to-disk-in-c/">Fastest Way to Write Text Files to Disk </see>
	internal class DiskWriter : IWrite
	{
		private const string ValueNotSpecified = @"n/a";

		private readonly string _destinationFilePath;
		private readonly Encoding _destinationFileEncoding;
		private readonly FileFormatType _fileFormatType;
		private readonly IRecordState _state;

		public DiskWriter(string destinationFilePath, Encoding destinationFileEncoding, FileFormatType fileFormatType, IRecordState state)
		{
			_state = state ?? throw new ArgumentNullException(nameof(state));

			if (string.IsNullOrWhiteSpace(destinationFilePath))
			{
				throw new ArgumentNullException(nameof(destinationFilePath), "Expected a directory path where the data will be saved.");
			}

			var targetDirectory = Path.GetDirectoryName(destinationFilePath);
			if (!System.IO.Directory.Exists(targetDirectory))
			{
				throw new ArgumentException("The given path does not refer to an existing directory.", nameof(targetDirectory));
			}

			_fileFormatType = fileFormatType;
			_destinationFileEncoding = destinationFileEncoding;
			_destinationFilePath = destinationFilePath;
		}

		public void Write(RecordList records)
		{
			Write(records, CancellationToken.None);
		}

		/// <remarks>
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and deletes the file saved so far (#1027).
		/// </remarks>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		public void Write(RecordList records, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(_destinationFilePath))
			{
				Log.Default.Write(LogSeverityType.Warning, "Records could not be saved - a destination has not been provided.");
			}
			else
			{
				if (records.Length == 0)
				{
					Log.Default.Write(LogSeverityType.Warning, $"Records could not be saved - no records have been provided. Count={records.Length}");
				}

				// Before an earlier file is replaced, so a save cancelled before it starts changes nothing.
				cancellationToken.ThrowIfCancellationRequested();

				if (System.IO.File.Exists(_destinationFilePath))
				{
					System.IO.File.Delete(_destinationFilePath);
				}

				Log.Default.Write(LogSeverityType.Debug, $"Records are being saved to disk... Format={_fileFormatType}");

				try
				{
					switch (_fileFormatType)
					{
						case FileFormatType.Raw:
							SaveAsRaw(records, cancellationToken);
							break;
						case FileFormatType.Tsv:
							SaveAsTsv(records, cancellationToken);
							break;
						default:
							throw new NotSupportedException($"The selected format type is not currently supported. Format={_fileFormatType}");
					}
				}
				catch (IOException e)
				{
					Log.Default.Write(LogSeverityType.Error, $"Records were not saved. Reason=`{e.Message}`");
					throw;
				}
				catch (OperationCanceledException)
				{
					Log.Default.Write(LogSeverityType.Information, $"Records were not saved - the save was cancelled. Format={_fileFormatType}");

					try
					{
						// The stream is closed by now, so the file saved so far can be deleted. It is the destination, never the log.
						System.IO.File.Delete(_destinationFilePath);
					}
					catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
					{
						// The caller is told of the cancel, not of a file that another application holds.
						Log.Default.Write(LogSeverityType.Warning, $"The records saved before the cancel could not be deleted. Reason=`{exception.Message}`");
					}

					throw;
				}

				Log.Default.Write($"Records have been saved. Format={_fileFormatType}, Count={records.Length}.");
			}
		}

		private void SaveAsRaw(RecordList records, CancellationToken cancellationToken)
		{
			using (var streamWriter = new StreamWriter(_destinationFilePath, false, _destinationFileEncoding, 65536))
			{
				// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
				var buffer = new ContentBuffer();

				foreach (IRecord record in records)
				{
					cancellationToken.ThrowIfCancellationRequested();
					streamWriter.WriteLine(buffer.Read(record));
				}
			}
		}

		private void SaveAsTsv(RecordList records, CancellationToken cancellationToken)
		{
			DateTime? previouslyCreatedAt = null;
			using (var streamWriter = new StreamWriter(_destinationFilePath, false, _destinationFileEncoding, 65536))
			{
				streamWriter.WriteLine("Line Number\tFlagged\tPinned\tComment\tElapsedTime\tCreated At\tContent");

				// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
				var buffer = new ContentBuffer();

				foreach (IRecord record in records)
				{
					cancellationToken.ThrowIfCancellationRequested();

					TimeSpan elapsedTime = TimeSpan.Zero;

					if (previouslyCreatedAt == null)
					{
						elapsedTime = TimeSpan.Zero;
					}
					else
					{
						elapsedTime = (record.CreatedAt - previouslyCreatedAt.Value);
					}

					var columns = string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t",
						record.LineNumber,
						_state.IsFlagged(record),
						_state.IsPinned(record),
						_state.HasComment(record) ? _state.GetComment(record) : ValueNotSpecified,
						elapsedTime.TotalSeconds.ToString("0.000"),
						record.HasCreationTime ? record.CreatedAt.ToString() : ValueNotSpecified);

					streamWriter.Write(columns);
					streamWriter.WriteLine(buffer.Read(record));

					if (record.HasCreationTime)
					{
						previouslyCreatedAt = record.CreatedAt;
					}
				}
			}
		}
	}
}
