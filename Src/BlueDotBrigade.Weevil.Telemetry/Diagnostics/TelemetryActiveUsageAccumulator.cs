namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	/// <summary>
	/// Accumulates active telemetry minutes using a capped activity lease window.
	/// </summary>
	public sealed class TelemetryActiveUsageAccumulator
	{
		private readonly double _maxLeaseMinutes;
		private DateTime? _lastActivityUtc;

		/// <summary>
		/// Initializes a new accumulator.
		/// </summary>
		/// <param name="activityLeaseDuration">
		/// The maximum elapsed interval to count for a single activity renewal.
		/// </param>
		public TelemetryActiveUsageAccumulator(TimeSpan activityLeaseDuration)
		{
			if (activityLeaseDuration <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(activityLeaseDuration));
			}

			_maxLeaseMinutes = activityLeaseDuration.TotalMinutes;
		}

		/// <summary>
		/// Gets the total accumulated active minutes.
		/// </summary>
		public double ActiveMinutes { get; private set; }

		/// <summary>
		/// Resets accumulated state and starts tracking from the provided timestamp.
		/// </summary>
		/// <param name="activityObservedAtUtc">The observed activity time in UTC.</param>
		public void Reset(DateTime activityObservedAtUtc)
		{
			ActiveMinutes = 0;
			_lastActivityUtc = activityObservedAtUtc;
		}

		/// <summary>
		/// Renews activity and adds a capped elapsed interval to active minutes.
		/// </summary>
		/// <param name="activityObservedAtUtc">The observed activity time in UTC.</param>
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
				ActiveMinutes += System.Math.Min(elapsed.TotalMinutes, _maxLeaseMinutes);
			}

			_lastActivityUtc = activityObservedAtUtc;
		}

		/// <summary>
		/// Clears all accumulated activity and tracking state.
		/// </summary>
		public void Clear()
		{
			ActiveMinutes = 0;
			_lastActivityUtc = null;
		}
	}
}
