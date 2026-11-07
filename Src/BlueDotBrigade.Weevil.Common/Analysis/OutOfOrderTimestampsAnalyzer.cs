namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Flags records whose timestamp is earlier than the previously observed timestamp beyond a configured tolerance.
	/// </summary>
	public class OutOfOrderTimestampsAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.Zero;

		private const string CommentLabel = "OutOfOrderTimestamps";

		public string Key => AnalysisType.OutOfOrderTimestamps.ToString();

		public string DisplayName => "Out-of-Order Timestamps";

		private Results Analyze(RecordList records, IRecordState state, TimeSpan tolerance, bool canUpdateMetadata)
		{
			var metric = new TemporalAnomalyMetrics(tolerance);

			var counter = 0;

			if (canUpdateMetadata)
			{
				// One step, by line number, so clearing a flag creates no record (#1150).
				state.SetFlagged(records, false);
			}

			var count = records.Count;

			for (var position = 0; position < count; position++)
			{
				// By line number and timestamp alone, so only a row that is flagged is created as a record (#1150).
				metric.Count(records.GetLineNumber(position), records.GetCreatedAt(position));

				if (canUpdateMetadata && metric.Counter != counter)
				{
					counter++;

					IRecord record = records[position];
					state.SetFlagged(record, true);
					state.AppendComment(record, $"{CommentLabel}: {metric.CurrentAnomaly.ToHumanReadable()}");
				}
			}

			return new Results(metric.Counter);
		}

		public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			Results results = Results.None;

			if (TryGetTolerance(userDialog, out TimeSpan tolerance))
			{
				results = Analyze(records, state, tolerance, canUpdateMetadata);
			}

			return results;
		}

		protected bool TryGetTolerance(IUserDialog user, out TimeSpan tolerance)
		{
			var userInput = user.ShowUserPrompt(
				"Out-of-Order Timestamps",
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
