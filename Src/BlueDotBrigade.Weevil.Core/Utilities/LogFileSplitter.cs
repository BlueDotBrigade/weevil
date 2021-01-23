namespace BlueDotBrigade.Weevil.Analysis.LogSplitter
{
	using System.Collections.Immutable;
	using System.IO;
	using System.Text;
	using Data;
	using Diagnostics;

	public class LogFileSplitter
	{
		private const string ApplicationStartMarker = "-----------------------------------------------";

		private readonly string _outputFileFormat;

		private readonly string _sourceFilePath;

		public LogFileSplitter(string sourceFilePath)
		{
			_sourceFilePath = sourceFilePath;
			_outputFileFormat = Path.GetFileNameWithoutExtension(sourceFilePath) + "-{0:00}.log";
		}

		public void Run(ImmutableArray<IRecord> records)
		{
			if (string.IsNullOrWhiteSpace(_sourceFilePath))
			{
				Log.Default.Write(LogSeverityType.Warning, "Application start analysis cannot be performed - log data has not been provided.");
			}
			else
			{
				if (records.Length == 0)
				{
					Log.Default.Write(LogSeverityType.Warning, "Application start analysis cannot be performed - no items have been selected.");
					// TODO: replace
					//MessageBox.Show("Please select items to analyze.");
				}
				else
				{
					Log.Default.Write("Application start analysis is starting...");

					var startCount = 0;
					var outputPath = Path.Combine(Path.GetDirectoryName(_sourceFilePath), string.Format(_outputFileFormat, startCount));

					var newLog = new StringBuilder();

					foreach (IRecord record in records)
					{
						if (record.Content.Contains(ApplicationStartMarker))
						{
							if (newLog.Length > 0)
							{
								if (File.Exists(outputPath))
								{
									File.Delete(outputPath);
								}
								File.WriteAllText(outputPath, newLog.ToString());
								newLog.Clear();
							}

							startCount++;
							outputPath = Path.Combine(Path.GetDirectoryName(_sourceFilePath), string.Format(_outputFileFormat, startCount));
						}

						newLog.AppendLine(record.Content);
					}

					if (newLog.Length > 0)
					{
						if (File.Exists(outputPath))
						{
							File.Delete(outputPath);
						}
						File.WriteAllText(outputPath, newLog.ToString());
						newLog.Clear();
					}

					Log.Default.Write("Application start analysis is complete.");
				}
			}
		}
	}
}
