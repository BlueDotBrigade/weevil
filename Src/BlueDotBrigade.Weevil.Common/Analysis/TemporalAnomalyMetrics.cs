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

		public TemporalAnomalyMetrics(TimeSpan threshold)
		{
			_threshold = threshold;
			Reset();
		}

		public IRecord FirstOccurredAt => _firstOccurredAt;

		public int Counter => _counter;

		public IRecord BiggestAnomalyAt => _biggestAnomalyAt;

		public TimeSpan BiggestAnomaly => _biggestAnomaly;

		public TimeSpan Thershold => _threshold;

		public void Count(IRecord record)
		{
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
						var period = _previousRecord.CreatedAt - record.CreatedAt;

						if (period > _threshold)
						{
							if (Record.IsDummyOrNull(_firstOccurredAt))
							{
								_firstOccurredAt = record;
							}
							Interlocked.Increment(ref _counter);

							if (period > _biggestAnomaly)
							{
								_biggestAnomaly = period;
								_biggestAnomalyAt = _previousRecord;
							}
						}
						else
						{
							var message = string.Format(
								"Ignoring temporal anomaly because it is less than the given threshold. Period={0}, Threshold={1}",
								period.ToHumanReadable(),
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
		}

		public IDictionary<string, object> GetResults()
		{
			return new Dictionary<string, object>
			{
				{ nameof(this.Counter), this.Counter },
				{ nameof(this.FirstOccurredAt), this.FirstOccurredAt.CreatedAt },
				{ nameof(this.BiggestAnomalyAt), this.BiggestAnomalyAt.CreatedAt },
				{ nameof(this.BiggestAnomaly), this.BiggestAnomaly },
			};
		}
	}
}