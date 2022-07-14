namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;

	internal class TemporalAnomalyMetrics : IMetricCollector
	{
		private IRecord _previousRecord;
		private IRecord _firstOccurredAt;

		private int _counter;

		private IRecord _biggestAnomalyAt;
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

		public IRecord FirstOccurredAt => _firstOccurredAt;

		public int Counter => _counter;

		public IRecord BiggestAnomalyAt => _biggestAnomalyAt;

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
			_currentAnomaly = TimeSpan.Zero;

			if (record.HasCreationTime)
			{
				if (Record.IsDummyOrNull(_previousRecord))
				{
					_previousRecord = record;
				}
				else
				{
					if (_previousRecord.CreatedAt <= record.CreatedAt)
					{
						// everything appears to be in order
					}
					else
					{
						_currentAnomaly = record.CreatedAt - _previousRecord.CreatedAt;
						
						if (_currentAnomaly < _threshold)
						{
							if (Record.IsDummyOrNull(_firstOccurredAt))
							{
								_firstOccurredAt = record;
							}
							Interlocked.Increment(ref _counter);

							if (_currentAnomaly < _biggestAnomaly)
							{
								_biggestAnomaly = _currentAnomaly;
								_biggestAnomalyAt = _previousRecord;
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
				_previousRecord = record;
			}
		}

		public void Reset()
		{
			_counter = 0;

			_previousRecord = Record.Dummy;
			_firstOccurredAt = Record.Dummy;
			_biggestAnomalyAt = Record.Dummy;

			_biggestAnomaly = TimeSpan.Zero;

			_currentAnomaly = TimeSpan.Zero;
		}

		public IDictionary<string, object> GetResults()
		{
			return new Dictionary<string, object>
			{
				{ nameof(this.Threshold), this.Threshold },
				{ nameof(this.Counter), this.Counter },
				{ nameof(this.FirstOccurredAt), this.FirstOccurredAt.CreatedAt },
				{ nameof(this.BiggestAnomalyAt), this.BiggestAnomalyAt.CreatedAt },
				{ nameof(this.BiggestAnomaly), this.BiggestAnomaly },
			};
		}
	}
}