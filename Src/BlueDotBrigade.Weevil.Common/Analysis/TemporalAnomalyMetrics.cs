namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	
	internal class TemporalAnomalyMetrics : IMetricCollector
	{
		private IRecord _previousRecord;
		private IRecord _firstOccurredAt;

		private int _counter;

		public TemporalAnomalyMetrics()
		{
			Reset();
		}

		public IRecord FirstOccurredAt => _firstOccurredAt;

		public int Counter => _counter;

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
						if (Record.IsDummyOrNull(_firstOccurredAt))
						{
							_firstOccurredAt = record;
						}
						Interlocked.Increment(ref _counter);
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
		}

		public IDictionary<string, object> GetResults()
		{
			return new Dictionary<string, object>
			{
				{ nameof(this.Counter), this.Counter },
				{ nameof(this.FirstOccurredAt), this.FirstOccurredAt },
			};
		}
	}
}
