﻿namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Analyzes a file looking for time periods where logging appears to have stopped.
	/// </summary>
	public class TimeGapUiAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(60);
		private const string CommentLabel = "TimeGapUi";

		private const int UnknownIndex = -1;

		private TimeSpan _maximumPeriodDetected;
		private int _count;
		private DateTime _firstOccurrenceAt;

		public TimeGapUiAnalyzer()
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_count = -1;
			_firstOccurrenceAt = DateTime.MaxValue;
		}

		public virtual string Key => AnalysisType.TimeGapUiOnly.ToString();

		public virtual string DisplayName => "Detect Time Gap (UI Only)";

		public TimeSpan MaximumPeriodDetected => _maximumPeriodDetected;

		public int Count => _count;

		public DateTime FirstOccurrenceAt => _firstOccurrenceAt;

		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			if (TryGetTolerance(userDialog, out TimeSpan maximumAllowedPeriod))
			{
				Analyze(records, maximumAllowedPeriod, canUpdateMetadata);
			}

			return this.Count;
		}

		private static int IndexOfFirstTimestamp(ImmutableArray<IRecord> records)
		{
			var result = UnknownIndex;

			for (var i = 0; i < records.Length; i++)
			{
				if (records[i].HasCreationTime && records[i].Metadata.WasGeneratedByUi)
				{
					result = i;
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Analyzes the <paramref name="records"/> looking for time periods where logging appears to have stopped.
		/// </summary>
		/// <remarks>
		/// The analysis is performed using actual <see cref="Record.CreatedAt"/> timestamps, 
		/// and not the <see cref="Metadata.ElapsedTime"/> value which is generated by the current view.
		/// </remarks>
		public int Analyze(ImmutableArray<IRecord> records, TimeSpan maximumAllowedPeriod, bool canUpdateMetadata)
		{
			Log.Default.Write(LogSeverityType.Debug, "Time gap analysis for records logged by the UI thread is starting...");

			_maximumPeriodDetected = TimeSpan.Zero;
			_count = 0;

			var previous = IndexOfFirstTimestamp(records);

			if (previous == UnknownIndex)
			{
				if (canUpdateMetadata)
				{
					foreach (IRecord record in records)
					{
						record.Metadata.IsFlagged = false;
					}
				}
			}
			else
			{
				for (var current = previous; current < records.Length; current++)
				{
					IRecord currentRecord = records[current];

					if (canUpdateMetadata)
					{
						currentRecord.Metadata.IsFlagged = false;
					}

					if (currentRecord.Metadata.WasGeneratedByUi)
					{
						CheckForTimeGap(currentRecord, records[previous], maximumAllowedPeriod, canUpdateMetadata);

						previous = current;
					}
				}
			}

			LogSeverityType severityType = _count > 0 ? LogSeverityType.Warning : LogSeverityType.Information;
			Log.Default.Write(severityType, $"Time gap analysis for records logged by the UI thread is complete. ProblemsDetected={_count}, MaximumPeriodDetected={_maximumPeriodDetected}, MaximumAllowedPeriod={maximumAllowedPeriod}");

			return _count;
		}

		protected virtual void CheckForTimeGap(IRecord currentRecord, IRecord previousRecord, TimeSpan maximumAllowedPeriod,
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
					currentRecord.Metadata.IsFlagged = true;

					currentRecord.Metadata.UpdateUserComment(
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

			unresponsivenessPeriod = wasSuccessful ? TimeSpan.FromMilliseconds(timePeriodInMs) : TimeSpan.Zero;

			return wasSuccessful;
		}
	}
}
