namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Diagnostics;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// Generates a log entry when the user interface (UI)
	/// is unresponsive for <see cref="TooSlowThreshold"/>.
	/// </summary>
	/// <seealso href="https://stackoverflow.com/q/11993530">StackOverflow: How to detect unresponsive UI updates</seealso>
	/// <seealso href="https://stackoverflow.com/a/7807903">Jon Skeet's input on DispatcherTimer</seealso>

	internal class UiResponsivenessMonitor
	{
		public static readonly TimeSpan TooSlowThreshold = TimeSpan.FromMilliseconds(1000);

		private static readonly TimeSpan SamplingPeriod = TimeSpan.FromMilliseconds(500);

		/// <summary>
		/// Indicates how much time must elapse before another log entry can be generated.
		/// </summary>
		private static readonly TimeSpan LoggingInterval = TimeSpan.FromMinutes(1);

		private readonly DispatcherTimer _dispatcherTimer;

		private DateTime _problemLastDetectedAt;
		private readonly Stopwatch _stopwatch;

		public UiResponsivenessMonitor()
		{
			_dispatcherTimer = new DispatcherTimer();
			_dispatcherTimer.Interval = SamplingPeriod;
			_dispatcherTimer.Tick += OnDispatcherTimerTick;

			_stopwatch = new Stopwatch();
		}

		~UiResponsivenessMonitor()
		{
			_dispatcherTimer.Tick -= OnDispatcherTimerTick;
		}

		private void OnDispatcherTimerTick(object sender, EventArgs e)
		{
			if (_stopwatch.Elapsed > TooSlowThreshold)
			{
				if (DateTime.Now.Subtract(_problemLastDetectedAt) > LoggingInterval)
				{
					Log.Default.Write(
						LogSeverityType.Warning, 
						$"The user interface (UI) appears to be unresponsive. EstimatedDelay={_stopwatch.Elapsed.ToHumanReadable()}");

					_problemLastDetectedAt = DateTime.Now;
				}
			}

			_stopwatch.Restart();
		}

		public void Start()
		{
			Log.Default.Write(
				LogSeverityType.Information,
				"Weevil is now monitoring it's user interace (UI) responsiveness.");

			_problemLastDetectedAt = DateTime.MinValue;
			_stopwatch.Start();
			_dispatcherTimer.Start();
		}

		public void Stop()
		{
			_problemLastDetectedAt = DateTime.MaxValue;
			_dispatcherTimer.Stop();
			_stopwatch.Stop();

			Log.Default.Write(
				LogSeverityType.Information,
				"Weevil is no longer monitoring it's user interace (UI) responsiveness.");
		}
	}
}
