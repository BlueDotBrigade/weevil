namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	public sealed class TelemetryActiveUsageAccumulator
	{
		private readonly double _maxLeaseMinutes;
		private DateTime? _lastActivityUtc;

		public TelemetryActiveUsageAccumulator(TimeSpan activityLeaseDuration)
		{
			if (activityLeaseDuration <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(activityLeaseDuration));
			}

			_maxLeaseMinutes = activityLeaseDuration.TotalMinutes;
		}

		public double ActiveMinutes { get; private set; }

		public void Reset(DateTime activityObservedAtUtc)
		{
			ActiveMinutes = 0;
			_lastActivityUtc = activityObservedAtUtc;
		}

		public void Renew(DateTime activityObservedAtUtc)
		{
			if (_lastActivityUtc is null)
			{
				_lastActivityUtc = activityObservedAtUtc;
				return;
			}

			var elapsed = activityObservedAtUtc - _lastActivityUtc.Value;
			if (elapsed > TimeSpan.Zero)
			{
				ActiveMinutes += Math.Min(elapsed.TotalMinutes, _maxLeaseMinutes);
			}

			_lastActivityUtc = activityObservedAtUtc;
		}

		public void Clear()
		{
			ActiveMinutes = 0;
			_lastActivityUtc = null;
		}
	}
}
