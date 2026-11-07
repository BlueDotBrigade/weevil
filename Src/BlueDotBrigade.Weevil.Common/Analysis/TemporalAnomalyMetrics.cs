namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;

	internal class TemporalAnomalyMetrics : IMetricCollector
	{
		// Where records were counted, not the records, so the metrics keep none of a log's content (#1139).
		private int _previousLineNumber;
		private DateTime _previousCreatedAt;
		private int _firstOccurredOnLine;
		private DateTime _firstOccurredAt;

		private int _counter;

		private int _biggestAnomalyOnLine;
		private DateTime _biggestAnomalyAt;
		private TimeSpan _biggestAnomaly;

		private readonly TimeSpan _threshold;

		private TimeSpan _currentAnomaly;

		/// <summary>
		/// Use to identify <see cref="IRecord"/> creation time anomalies.
		/// </summary>
		/// <param name="threshold">
		/// Threshold used to determine if a problem should be reported.
		/// When the timestamps are not in chronological order, the threshold should be expressed as a negative value.</param>
		public TemporalAnomalyMetrics(TimeSpan threshold)
		{
			_threshold = threshold;

			Reset();
		}

		/// <summary>
		/// When the first anomaly's record was created, or <see cref="Record.CreationTimeUnknown"/>.
		/// </summary>
		public DateTime FirstOccurredAt => _firstOccurredAt;

		/// <summary>
		/// The line number of the first anomaly's record, or the line number of <see cref="Record.Dummy"/>.
		/// </summary>
		public int FirstOccurredOnLine => _firstOccurredOnLine;

		public int Counter => _counter;

		/// <summary>
		/// When the record before the biggest anomaly was created, or <see cref="Record.CreationTimeUnknown"/>.
		/// </summary>
		public DateTime BiggestAnomalyAt => _biggestAnomalyAt;

		/// <summary>
		/// The line number of the record before the biggest anomaly, or the line number of <see cref="Record.Dummy"/>.
		/// </summary>
		public int BiggestAnomalyOnLine => _biggestAnomalyOnLine;

		public TimeSpan BiggestAnomaly => _biggestAnomaly;

		/// <summary>
		/// Indicates how much of an error must exist before a problem is detected.
		/// </summary>
		/// <remarks>
		/// Value is expected to be less than or equal to zero.
		/// </remarks>
		public TimeSpan Threshold => _threshold;

		public TimeSpan CurrentAnomaly => _currentAnomaly;

		public void Count(IRecord record)
		{
			Count(record.LineNumber, record.CreatedAt);
		}

		/// <summary>
		/// Counts the record on <paramref name="lineNumber"/>, created at <paramref name="createdAt"/>, without the record itself (#1150).
		/// </summary>
		public void Count(int lineNumber, DateTime createdAt)
		{
			_currentAnomaly = TimeSpan.Zero;

			if (createdAt != Record.CreationTimeUnknown)
			{
				if (_previousLineNumber == Record.Dummy.LineNumber)
				{
					// The first record with a timestamp: nothing to compare it with yet.
				}
				else
				{
					if (_previousCreatedAt <= createdAt)
					{
						// everything appears to be in order
					}
					else
					{
						_currentAnomaly = createdAt - _previousCreatedAt;
						
						if (_currentAnomaly < _threshold)
						{
							if (_firstOccurredOnLine == Record.Dummy.LineNumber)
							{
								_firstOccurredOnLine = lineNumber;
								_firstOccurredAt = createdAt;
							}
							Interlocked.Increment(ref _counter);

							if (_currentAnomaly < _biggestAnomaly)
							{
								_biggestAnomaly = _currentAnomaly;
								_biggestAnomalyOnLine = _previousLineNumber;
								_biggestAnomalyAt = _previousCreatedAt;
							}
						}
						else
						{
							var message = string.Format(
								"Ignoring temporal anomaly because it is less than the given threshold. Period={0}, Threshold={1}",
								_currentAnomaly.ToHumanReadable(),
								_threshold.ToHumanReadable());
						
							Log.Default.Write(LogSeverityType.Information, message);
						}
					}
				}
				_previousLineNumber = lineNumber;
				_previousCreatedAt = createdAt;
			}
		}

		public void Reset()
		{
			_counter = 0;

			_previousLineNumber = Record.Dummy.LineNumber;
			_previousCreatedAt = Record.CreationTimeUnknown;
			_firstOccurredOnLine = Record.Dummy.LineNumber;
			_firstOccurredAt = Record.CreationTimeUnknown;
			_biggestAnomalyOnLine = Record.Dummy.LineNumber;
			_biggestAnomalyAt = Record.CreationTimeUnknown;

			_biggestAnomaly = TimeSpan.Zero;

			_currentAnomaly = TimeSpan.Zero;
		}

		public IDictionary<string, object> GetResults()
		{
			return new Dictionary<string, object>
			{
				{ nameof(this.Threshold), this.Threshold },
				{ nameof(this.Counter), this.Counter },
				{ nameof(this.FirstOccurredAt), this.FirstOccurredAt },
				{ nameof(this.BiggestAnomalyAt), this.BiggestAnomalyAt },
				{ nameof(this.BiggestAnomaly), this.BiggestAnomaly },
			};
		}
	}
}