namespace BlueDotBrigade.Weevil.Utilities
{
	using System;
	using System.IO;
	using System.Text;
	using System.Threading;
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

		public void Run(RecordList records)
		{
			Run(records, CancellationToken.None);
		}

		/// <summary>
		/// As <see cref="Run(RecordList)"/>, and stops within one record when <paramref name="cancellationToken"/> is cancelled, as Abort does (#1028).
		/// </summary>
		/// <remarks>
		/// The files already written stay. The file being collected when the split stops is not written.
		/// </remarks>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		public void Run(RecordList records, CancellationToken cancellationToken)
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

					// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
					var buffer = new ContentBuffer();

					foreach (IRecord record in records)
					{
						cancellationToken.ThrowIfCancellationRequested();

						ReadOnlySpan<char> content = buffer.Read(record);

						if (content.Contains(ApplicationStartMarker, StringComparison.Ordinal))
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

						newLog.Append(content).AppendLine();
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
