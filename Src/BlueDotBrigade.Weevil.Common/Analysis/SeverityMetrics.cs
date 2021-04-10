namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;

	public class SeverityMetrics : IMetricCollector
	{
		#region Fields
		private int _information;
		private int _warning;
		private int _error;
		private int _fatal;

		private IRecord _fatalFirstOccurredAt;
		#endregion

		#region Object Lifetime
		#endregion

		#region Properties
		public int Information => _information;

		public int Warnings => _warning;

		public int Errors => _error;

		public int Fatals => _fatal;

		public IRecord FatalFirstOccurredAt => _fatalFirstOccurredAt;
		#endregion

		#region Private Methods
		#endregion

		#region Event Handlers
		#endregion

		public void Count(IRecord record)
		{
			switch (record.Severity)
			{
				case SeverityType.Information:
					Interlocked.Increment(ref _information); // TODO: remove Increment() to improve performance
					break;

				case SeverityType.Warning:
					Interlocked.Increment(ref _warning);
					break;

				case SeverityType.Error:
					Interlocked.Increment(ref _error);
					break;

				case SeverityType.Critical:
					var count = Interlocked.Increment(ref _fatal);
					if (count == 1)
					{
						_fatalFirstOccurredAt = record;
					}
					break;
			}
		}

		public void Reset()
		{
			Interlocked.Exchange(ref _information, 0);
			Interlocked.Exchange(ref _warning, 0);
			Interlocked.Exchange(ref _error, 0);
			Interlocked.Exchange(ref _fatal, 0);
		}

		public IDictionary<string, object> GetResults()
		{
			return new Dictionary<string, object>
			{
				{ nameof(this.Information), this.Information },
				{ nameof(this.Warnings), this.Warnings },
				{ nameof(this.Errors), this.Errors },
				{ nameof(this.Fatals), this.Fatals },
			};
		}

		public override string ToString()
		{
			return $"Information={_information}, Warning={_warning}, Error={_error}, Critical={_fatal}";
		}

		public static SeverityMetrics operator +(SeverityMetrics left, SeverityMetrics right)
		{
			return new SeverityMetrics
			{
				_information = left.Information + right.Information,
				_warning = left.Warnings + right.Warnings,
				_error = left.Errors + right.Errors,
				_fatal = left.Fatals + right.Fatals,
			};
		}

		public static SeverityMetrics operator -(SeverityMetrics left, SeverityMetrics right)
		{
			return new SeverityMetrics
			{
				_information = left.Information - right.Information,
				_warning = left.Warnings - right.Warnings,
				_error = left.Errors - right.Errors,
				_fatal = left.Fatals - right.Fatals,
			};
		}
	}
}
