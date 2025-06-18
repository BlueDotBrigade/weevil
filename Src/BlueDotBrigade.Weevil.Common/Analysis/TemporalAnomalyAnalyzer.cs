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

		private Results Analyze(ImmutableArray<IRecord> records, TimeSpan tolerance, bool canUpdateMetadata)
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
						record.Metadata.UpdateUserComment($"{CommentLabel}: {metric.CurrentAnomaly.ToHumanReadable()}");
					}
				}
			}

			return new Results(metric.Counter);
		}

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			Results results = Results.None;

			if (TryGetTolerance(userDialog, out TimeSpan tolerance))
			{
				results = Analyze(records, tolerance, canUpdateMetadata);
			}

			return results;
		}

		protected bool TryGetTolerance(IUserDialog user, out TimeSpan tolerance)
		{
			var userInput = user.ShowUserPrompt(
				"Temporal Anomaly",
				"Threshold (ms):",
				DefaultThreshold.TotalMilliseconds.ToString("-0"));

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