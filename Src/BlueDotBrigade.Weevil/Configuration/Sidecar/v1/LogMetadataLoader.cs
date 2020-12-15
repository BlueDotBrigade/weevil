namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v1
{
	using System;
	using System.Collections.Immutable;
	using System.Runtime.Serialization;
	using Data;
	using Diagnostics;
	using IO;
	using Weevil.Collections.Immutable;
	using Runtime.Serialization;

	internal class LogMetadataLoader : ISidecarLoader
	{
		private readonly IFile _file;
		private readonly string _filePath;
		private LogMetadata _metadata;

		public LogMetadataLoader(string filePath)
		{
			_filePath = filePath;
			_file = new File();
		}

		public bool Load()
		{
			var canLoad = false;

			try
			{
				if (_file.Exists(_filePath))
				{
					FixNamespaceCompatibilityIssues(_file, _filePath);

					_metadata = TypeFactory.LoadFromXml<LogMetadata>(_filePath);
					canLoad = true;
				}
			}
			catch (SerializationException e)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					e,
					"Sidecar data could not be loaded. The file format may not be compatible with this version of Log Viewer.");
			}

			return canLoad;
		}

		public void Apply(ImmutableArray<IRecord> allRecords)
		{
			foreach (Label label in _metadata.Labels)
			{
				if (allRecords.TryGetLine(label.LineNumber, out IRecord record))
				{
					if (!string.IsNullOrWhiteSpace(record.Metadata.Comment))
					{
						Log.Default.Write(
							LogSeverityType.Warning,
							$"Overwriting current user comment with the value found in the sidecar. LineNumber={label.LineNumber}");
					}
					record.Metadata.Comment = label.Name;
				}
				else
				{
					Log.Default.Write(
						LogSeverityType.Warning,
						$"Sidecar references a record that cannot be found in this log file. LineNumber={label.LineNumber}");
				}
			}
		}

		public void Save(bool deleteBackup, ImmutableArray<IRecord> allRecords)
		{
			throw new NotSupportedException("A deprecated schema cannot be used to save sidecar data.");
		}

		private static void FixNamespaceCompatibilityIssues(IFile file, string filePath)
		{
			const string NewSchemaName = @"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v1";

			var xmlContent = file.ReadAllText(filePath);
			xmlContent = xmlContent.Replace("http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Data.Document", NewSchemaName);
			xmlContent = xmlContent.Replace("http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Document", NewSchemaName);

			file.WriteAllText(filePath, xmlContent);
		}
	}
}
