namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Analyzes a file looking for time periods where logging appears to have stopped.
	/// </summary>
	/// <remarks>
	/// If a <see cref="IRecord"/> does not have a timestamp,
	/// the analyzer will treat it as though it didn't exist.
	/// </remarks>
	public class TimeGapAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(60);

		private TimeSpan _maximumPeriodDetected;
		private int _count;
		private DateTime _firstOccurrenceAt;
		private int _maximumPeriodLineNumber;

		/// <summary>
		/// Analyzes a file looking for time periods where logging appears to have stopped.
		/// </summary>
		/// <remarks>
		/// If a <see cref="IRecord"/> does not have a timestamp,
		/// the analyzer will treat it as though it didn't exist.
		/// </remarks>
		public TimeGapAnalyzer()
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_count = -1;
			_firstOccurrenceAt = DateTime.MaxValue;
			_maximumPeriodLineNumber = -1;
		}

		public virtual string Key => AnalysisType.ElapsedTime.ToString();

		public virtual string DisplayName => "Measure Elapsed Time";

		public TimeSpan MaximumPeriodDetected => _maximumPeriodDetected;

		public int Count => _count;

		public DateTime FirstOccurrenceAt => _firstOccurrenceAt;

		/// <summary>
		/// The line of the record that ends the longest period, <see cref="MaximumPeriodDetected"/>, or -1 when there is none (#1226).
		/// </summary>
		public int MaximumPeriodLineNumber => _maximumPeriodLineNumber;

		/// <summary>
		/// Begins the comment of each record that ends a gap.
		/// </summary>
		protected virtual string CommentLabel => "ElapsedTime";

		public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_count = 0;
			_firstOccurrenceAt = DateTime.MaxValue;
			_maximumPeriodLineNumber = -1;

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
			Log.Default.Write(LogSeverityType.Debug, $"Time gap analysis is starting... Analyzer={this.Key}");

			_maximumPeriodDetected = TimeSpan.Zero;
			_count = 0;
			_firstOccurrenceAt = DateTime.MaxValue;
			_maximumPeriodLineNumber = -1;

			if (canUpdateMetadata)
			{
				// One step, by line number, so clearing a flag creates no record (#1150).
				state.SetFlagged(records, false);
			}

			// Each timestamped row that can be measured is compared with the one before it, and the first with itself,
			// by its timestamp and facts alone (#1140, #1150).
			DateTime previousCreatedAt = Record.CreationTimeUnknown;
			var count = records.Count;

			for (var position = 0; position < count; position++)
			{
				DateTime createdAt = records.GetCreatedAt(position);

				if (createdAt != Record.CreationTimeUnknown && CanMeasure(records, position))
				{
					DateTime comparedWith = previousCreatedAt != Record.CreationTimeUnknown ? previousCreatedAt : createdAt;
					CheckForTimeGap(records, position, createdAt, comparedWith, state, maximumAllowedPeriod, canUpdateMetadata);
					previousCreatedAt = createdAt;
				}
			}

			LogSeverityType severityType = _count > 0 ? LogSeverityType.Warning : LogSeverityType.Information;
			Log.Default.Write(severityType, $"Time gap analysis is complete. Analyzer={this.Key}, ProblemsDetected={_count}, MaximumPeriodDetected={_maximumPeriodDetected}, MaximumAllowedPeriod={maximumAllowedPeriod}");

			return new Results(_count);
		}

		/// <summary>
		/// Returns whether the timestamped row at <paramref name="position"/> is measured. A row that is not is ignored,
		/// and is not compared with the next. Every row is measured by default.
		/// </summary>
		/// <remarks>
		/// Read the row with <see cref="RecordList.GetFacts(int)"/> or another method that creates no record (#1150).
		/// </remarks>
		protected virtual bool CanMeasure(RecordList records, int position)
		{
			return true;
		}

		/// <param name="records">The records being analyzed.</param>
		/// <param name="position">The row of <paramref name="records"/> to check. Its record is created only if it is flagged (#1150).</param>
		/// <param name="createdAt">When the row's record was created.</param>
		/// <param name="previousCreatedAt">When the timestamped row before it was created, or <paramref name="createdAt"/> for the first one.</param>
		protected virtual void CheckForTimeGap(RecordList records, int position, DateTime createdAt, DateTime previousCreatedAt, IRecordState state,
			TimeSpan maximumAllowedPeriod, bool canUpdateMetadata)
		{
			TimeSpan elapsedTime = createdAt - previousCreatedAt;

			if (elapsedTime > _maximumPeriodDetected)
			{
				_maximumPeriodDetected = elapsedTime;
				_maximumPeriodLineNumber = records.GetLineNumber(position);
			}

			if (elapsedTime > maximumAllowedPeriod)
			{
				_count++;

				if (_count == 1)
				{
					_firstOccurrenceAt = createdAt;
				}

				if (canUpdateMetadata)
				{
					IRecord currentRecord = records[position];

					state.SetFlagged(currentRecord, true);

					state.AppendComment(						currentRecord,
						$"{this.CommentLabel}: {elapsedTime.ToHumanReadable()}");
				}
			}
		}

		protected virtual bool TryGetTolerance(IUserDialog user, out TimeSpan unresponsivenessPeriod)
		{
			return TryGetTolerance(user, "Elapsed Time", "Threshold (ms):", out unresponsivenessPeriod);
		}

		/// <summary>
		/// Asks the user for the longest period, in milliseconds, that is not a gap.
		/// </summary>
		protected static bool TryGetTolerance(IUserDialog user, string title, string prompt, out TimeSpan unresponsivenessPeriod)
		{
			var userInput = user.ShowUserPrompt(
				title,
				prompt,
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
