namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.IO;
	using System.Text;
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
		private readonly FileFormatType _fileFormatType;

		public DiskWriter(string destinationFilePath, FileFormatType fileFormatType)
		{
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

			_destinationFilePath = destinationFilePath;
		}

		public void Write(ImmutableArray<IRecord> records)
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
							SaveAsRaw(records);
							break;
						case FileFormatType.Tsv:
							SaveAsTsv(records);
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

				Log.Default.Write($"Records have been saved. Format={_fileFormatType}, Count={records.Length}.");
			}
		}

		private void SaveAsRaw(ImmutableArray<IRecord> records)
		{
			using (var streamWriter = new StreamWriter(_destinationFilePath, false, Encoding.UTF8, 65536))
			{
				foreach (IRecord record in records)
				{
					streamWriter.WriteLine(record.Content);
				}
			}
		}

		private void SaveAsTsv(ImmutableArray<IRecord> records)
		{
			DateTime? previouslyCreatedAt = null;
			using (var streamWriter = new StreamWriter(_destinationFilePath, false, Encoding.UTF8, 65536))
			{
				streamWriter.WriteLine("Line Number\tFlagged\tPinned\tComment\tElapsedTime\tCreated At\tContent");

				foreach (IRecord record in records)
				{
					TimeSpan elapsedTime = TimeSpan.Zero;

					if (previouslyCreatedAt == null)
					{
						elapsedTime = TimeSpan.Zero;
					}
					else
					{
						elapsedTime = (record.CreatedAt - previouslyCreatedAt.Value);
					}

					var serializedData = string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
						record.LineNumber,
						record.Metadata.IsFlagged,
						record.Metadata.IsPinned,
						record.Metadata.HasComment ? record.Metadata.Comment : ValueNotSpecified,
						elapsedTime.TotalSeconds.ToString("0.000"),
						record.HasCreationTime ? record.CreatedAt.ToString() : ValueNotSpecified,
						record.Content);

					streamWriter.WriteLine(serializedData);

					if (record.HasCreationTime)
					{
						previouslyCreatedAt = record.CreatedAt;
					}
				}
			}
		}
	}
}
