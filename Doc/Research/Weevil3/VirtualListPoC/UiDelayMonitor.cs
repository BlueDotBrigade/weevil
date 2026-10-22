#nullable enable

namespace Weevil.VirtualListPoC;

using System.Diagnostics;
using System.Windows.Threading;

/// <summary>
/// Measures how long the UI thread keeps the window from responding: every 50 ms a background timer queues
/// a tiny task on the dispatcher, and the wait before it runs is the delay a key press would see.
/// </summary>
internal sealed class UiDelayMonitor : IDisposable
{
	private readonly Dispatcher _dispatcher;
	private readonly Timer _timer;
	private readonly object _padlock = new object();
	private long _sentAt;
	private int _isPending;
	private double _longestMilliseconds;

	public UiDelayMonitor(Dispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		_timer = new Timer(_ => Probe(), null, 50, 50);
	}

	/// <summary>
	/// The longest delay since the last <see cref="Reset"/>, in milliseconds.
	/// </summary>
	public double LongestMilliseconds
	{
		get
		{
			lock (_padlock)
			{
				return _longestMilliseconds;
			}
		}
	}

	public void Reset()
	{
		lock (_padlock)
		{
			_longestMilliseconds = 0;
		}
	}

	public void Dispose()
	{
		_timer.Dispose();
	}

	private void Probe()
	{
		// While a probe is still waiting, the UI thread is busy: its delay is measured when it finally runs.
		if (Interlocked.Exchange(ref _isPending, 1) == 1)
		{
			return;
		}

		Volatile.Write(ref _sentAt, Stopwatch.GetTimestamp());
		_dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(OnProbe));
	}

	private void OnProbe()
	{
		var delay = Stopwatch.GetElapsedTime(Volatile.Read(ref _sentAt)).TotalMilliseconds;
		lock (_padlock)
		{
			_longestMilliseconds = System.Math.Max(_longestMilliseconds, delay);
		}

		Volatile.Write(ref _isPending, 0);
	}
}
