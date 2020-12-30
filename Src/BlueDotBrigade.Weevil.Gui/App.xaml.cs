namespace BlueDotBrigade.Weevil.Gui
{
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Diagnostics;

	public partial class App : Application
	{
		public App()
		{
			Startup += OnApplicationOpening;
			Exit += OnApplicationClosing;
		}

		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			var errorMsg = string.Format("An exception thrown by the WPF UI thread does not have an appropriate handler. IsHandled={0}", e.Handled);

			try
			{
				if (e.Exception == null)
				{
					Log.Default.Write(LogSeverityType.Critical, errorMsg);
				}
				else
				{
					Log.Default.Write(LogSeverityType.Critical, e.Exception, errorMsg);
				}
			}
			catch
			{
				// Nothing to do
				// ... memory allocations and method calls should be kept to a minimum
			}
		}

		private void OnApplicationOpening(object sender, StartupEventArgs e)
		{
			try
			{
				Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

				Log.Default.Write(LogSeverityType.Information, "Weevil application is starting...");
				Log.Register(new NLogWriter());
				Log.Default.Write(LogSeverityType.Debug, $"The logging library has been registered. Type={nameof(NLogWriter)}");
			}
			finally
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"Weevil application has started. Arguments={e.Args.Length}");
			}
		}

		private void OnApplicationClosing(object sender, ExitEventArgs e)
		{
			Log.Default.Write(
				LogSeverityType.Information,
				"Weevil application is closing...");
		}
	}
}