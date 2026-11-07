namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Analyzes timestamped UI-thread records for periods where UI logging appears to have stopped.
	/// </summary>
	/// <remarks>
	/// Untimestamped and non-UI records are ignored and do not replace the previous comparison record.
	/// </remarks>
	public class TimeGapUiAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(60);
		private const string CommentLabel = "ElapsedTimeUi";

		private TimeSpan _maximumPeriodDetected;
		private int _count;
		private DateTime _firstOccurrenceAt;

		public TimeGapUiAnalyzer()
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_count = -1;
			_firstOccurrenceAt = DateTime.MaxValue;
		}

		public virtual string Key => AnalysisType.ElapsedTimeUiThread.ToString();

		public virtual string DisplayName => "Measure Elapsed Time (UI Only)";

		public TimeSpan MaximumPeriodDetected => _maximumPeriodDetected;

		public int Count => _count;

		public DateTime FirstOccurrenceAt => _firstOccurrenceAt;

		public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_count = 0;
			_firstOccurrenceAt = DateTime.MaxValue;

			Results results = Results.None;

			if (TryGetTolerance(userDialog, out TimeSpan maximumAllowedPeriod))
			{
				results = Analyze(records, state, maximumAllowedPeriod, canUpdateMetadata);
			}

			return results;
		}

		/// <summary>
		/// Analyzes the <paramref name="records"/> looking for time periods where logging appears to have stopped.
		/// </summary>
		/// <remarks>
		/// The analysis is performed using actual <see cref="Record.CreatedAt"/> timestamps, 
		/// and not the elapsed times of the current view (<see cref="IRecordState.GetElapsedTime"/>).
		/// </remarks>
		/// <param name="state">Not used when <paramref name="canUpdateMetadata"/> is <see langword="false"/>.</param>
		public Results Analyze(RecordList records, IRecordState state, TimeSpan maximumAllowedPeriod, bool canUpdateMetadata)
		{
			Log.Default.Write(LogSeverityType.Debug, "Time gap analysis for records logged by the UI thread is starting...");

			_maximumPeriodDetected = TimeSpan.Zero;
			_count = 0;
			_firstOccurrenceAt = DateTime.MaxValue;

			if (canUpdateMetadata)
			{
				foreach (IRecord record in records)
				{
					state.SetFlagged(record, false);
				}
			}

			// Each timestamped record is created once, and each UI record is compared with the UI record before it; the first is compared with itself (#1140).
			IRecord previousRecord = null;

			for (var position = 0; position < records.Length; position++)
			{
				if (records.GetCreatedAt(position) != Record.CreationTimeUnknown)
				{
					IRecord currentRecord = records[position];

					if (currentRecord.WasGeneratedByUi)
					{
						CheckForTimeGap(currentRecord, previousRecord ?? currentRecord, state, maximumAllowedPeriod, canUpdateMetadata);

						previousRecord = currentRecord;
					}
				}
			}

			LogSeverityType severityType = _count > 0 ? LogSeverityType.Warning : LogSeverityType.Information;
			Log.Default.Write(severityType, $"Time gap analysis for records logged by the UI thread is complete. ProblemsDetected={_count}, MaximumPeriodDetected={_maximumPeriodDetected}, MaximumAllowedPeriod={maximumAllowedPeriod}");

			return new Results(_count);
		}

		protected virtual void CheckForTimeGap(IRecord currentRecord, IRecord previousRecord, IRecordState state, TimeSpan maximumAllowedPeriod,
			bool canUpdateMetadata)
		{
			TimeSpan elapsedTime = currentRecord.CreatedAt - previousRecord.CreatedAt;

			_maximumPeriodDetected = elapsedTime > _maximumPeriodDetected
				? elapsedTime
				: _maximumPeriodDetected;

			if (elapsedTime > maximumAllowedPeriod)
			{
				_count++;

				if (_count == 1)
				{
					_firstOccurrenceAt = currentRecord.CreatedAt;
				}

				if (canUpdateMetadata)
				{
					state.SetFlagged(currentRecord, true);

					state.AppendComment(						currentRecord,
						$"{CommentLabel}: {TimeSpanExtensions.ToHumanReadable(elapsedTime)}");
				}
			}
		}

		protected virtual bool TryGetTolerance(IUserDialog user, out TimeSpan unresponsivenessPeriod)
		{
			var userInput = user.ShowUserPrompt(
				"Input Required",
				"Maximum delay (ms):",
				DefaultThreshold.TotalMilliseconds.ToString("0.#"));

			var wasSuccessful = int.TryParse(userInput, out var timePeriodInMs);

			if (!wasSuccessful)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					$"Unable to perform the temporal analysis because an unexpected input was received. Input={userInput}");
			}

			unresponsivenessPeriod = wasSuccessful ? TimeSpan.FromMilliseconds(timePeriodInMs) : TimeSpan.Zero;

			return wasSuccessful;
		}
	}
}
