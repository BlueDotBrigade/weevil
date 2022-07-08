namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	public class TemporalAnomalyAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.Zero;

		private const string CommentLabel = "TemporalAnomaly";

		public string Key => AnalysisType.TemporalAnomaly.ToString();

		public string DisplayName => "Temporal Anomaly";

		private int Analyze(ImmutableArray<IRecord> records, TimeSpan tolerance, bool canUpdateMetadata)
		{
			var metric = new TemporalAnomalyMetrics(tolerance);

			var counter = 0;

			foreach (IRecord record in records)
			{
				metric.Count(record);

				if (canUpdateMetadata)
				{
					if (metric.Counter == counter)
					{
						record.Metadata.IsFlagged = false;
					}
					else
					{
						counter++;

						record.Metadata.IsFlagged = true;
						record.Metadata.UpdateUserComment($"{CommentLabel}");
					}
				}
			}

			return metric.Counter;
		}

		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			if (TryGetTolerance(userDialog, out TimeSpan tolerance))
			{
				count = Analyze(records, tolerance, canUpdateMetadata);
			}

			return count;
		}

		protected bool TryGetTolerance(IUserDialog user, out TimeSpan tolerance)
		{
			var userInput = user.ShowUserPrompt(
				"Temporal Anomaly",
				"Threshold (ms):",
				DefaultThreshold.TotalMilliseconds.ToString("0.#"));

			var wasSuccessful = int.TryParse(userInput, out var timePeriodInMs);

			if (!wasSuccessful)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					$"Unable to perform the temporal analysis because an unexpected input was received. Input={userInput}");
			}

			tolerance = wasSuccessful ? TimeSpan.FromMilliseconds(timePeriodInMs) : TimeSpan.Zero;

			return wasSuccessful;
		}
	}
}