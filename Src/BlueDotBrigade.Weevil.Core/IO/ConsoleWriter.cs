namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.IO;
	using Analysis;
	using Data;
	using Diagnostics;

	/// <summary>
	/// Produces a report that includes the amount of time that has elapsed between the selected log entries.
	/// </summary>
	public class ConsoleWriter : IWrite
	{
		private const string ValueNotSpecified = @"n\a";

		private readonly FileFormatType _fileFormatType;

		public ConsoleWriter(FileFormatType fileFormatType)
		{
			_fileFormatType = fileFormatType;
		}

		public void Write(ImmutableArray<IRecord> records)
		{
			if (records.Length == 0)
			{
				Log.Default.Write(LogSeverityType.Warning, $"Records could not be written - no records have been provided. Count={records.Length}");
			}

			Log.Default.Write(LogSeverityType.Debug, $"Records are written... Format={_fileFormatType}");

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
						throw new NotSupportedException($"The selected format type is not supported. Format={_fileFormatType}");
				}
			}
			catch (IOException e)
			{
				Log.Default.Write(LogSeverityType.Error, $"Records could not be written. FormatType={_fileFormatType} Reason=`{e.Message}`");
				throw;
			}

			Log.Default.Write($"Records have been saved. Format={_fileFormatType}, Count={records.Length}.");
		}

		private void SaveAsRaw(ImmutableArray<IRecord> records)
		{
			foreach (IRecord record in records)
			{
				Console.WriteLine(record.Content);
			}
		}

		private void SaveAsTsv(ImmutableArray<IRecord> records)
		{
			Console.WriteLine("Line Number\tFlagged\tComment\tCreatedAt\tElapsedTime\tContent");

			DateTime? previouslyCreatedAt = null;

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

				var serializedData = string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
					record.LineNumber,
					record.Metadata.IsFlagged,
					record.Metadata.HasComment ? record.Metadata.Comment : ValueNotSpecified,
					record.HasCreationTime ? record.CreatedAt.ToString() : ValueNotSpecified,
					elapsedTime.TotalSeconds.ToString("0.000"),
					record.Content);

				Console.WriteLine(serializedData);

				if (record.HasCreationTime)
				{
					previouslyCreatedAt = record.CreatedAt;
				}
			}
		}
	}
}