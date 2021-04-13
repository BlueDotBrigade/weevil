﻿namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Diagnostics;
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Threading.Tasks;
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
			var errorMessage = string.Format("An exception thrown by the WPF UI thread does not have an appropriate handler. IsHandled={0}", e.Handled);

			try
			{
				if (e.Exception == null)
				{
					Log.Default.Write(LogSeverityType.Critical, errorMessage);
				}
				else
				{
					Log.Default.Write(LogSeverityType.Critical, e.Exception, errorMessage);
				}
			}
			catch
			{
				// Nothing to do
				// ... memory allocations and method calls should be kept to a minimum
			}
		}

		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;

			var errorMessage = string.Format("An unhandled .Net exception is forcing the application to terminate unexpectedly.");

			try
			{
				if (exception == null)
				{
					Log.Default.Write(LogSeverityType.Critical, errorMessage);
				}
				else
				{
					Log.Default.Write(LogSeverityType.Critical, exception, errorMessage);
				}
			}
			catch
			{
				// Nothing to do
				// ... memory allocations and method calls should be kept to a minimum
			}
		}

		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		private void OnUnhandledTplException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			var errorMessage = string.Format("An unhandled TPL exception is forcing the application to terminate unexpectedly.");

			try
			{
				if (e.Exception == null)
				{
					Log.Default.Write(LogSeverityType.Critical, errorMessage);
				}
				else
				{
					Log.Default.Write(LogSeverityType.Critical, e.Exception, errorMessage);
				}
			}
			catch
			{
				// Nothing to do
				// ... memory allocations and method calls should be kept to a minimum
			}
		}

		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		private void OnApplicationOpening(object sender, StartupEventArgs e)
		{
			try
			{
				Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
				TaskScheduler.UnobservedTaskException += OnUnhandledTplException;

				Log.Default.Write(LogSeverityType.Information, "Weevil application is registering the logginer library...");
				Log.Register(new NLogWriter());

				Log.Default.Write(
					LogSeverityType.Information,
					"Weevil application is starting...");

				Log.Default.Write(LogSeverityType.Debug,
					$"The logging library has been registered. Type={nameof(NLogWriter)}");

				if (Debugger.IsAttached)
				{
					Log.Default.Write(LogSeverityType.Warning,
						"Visual Studio debugger is attached to this instance of Weevil.");
				}
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