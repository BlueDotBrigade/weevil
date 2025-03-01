namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Diagnostics;
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Properties;

	public partial class App : Application
	{
		public App()
		{
			Startup += OnApplicationOpening;
			Exit += OnApplicationClosing;
		}

		[SecurityCritical]
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
		private void OnApplicationOpening(object sender, StartupEventArgs e)
		{
			try
			{
				Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
				TaskScheduler.UnobservedTaskException += OnUnhandledTplException;

				Log.Default.Write(LogSeverityType.Information, "Weevil application is initializing logging...");
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

				var computerSnapshot = ComputerSnapshot.Create();

				var computerDetails =  
					$"OsName=`{computerSnapshot.OsName}`, " +
					$"OsIs64Bit={computerSnapshot.OsIs64Bit}, " +
					$"CpuName=`{computerSnapshot.CpuName}`, " +
					$"RamTotalInstalled={computerSnapshot.RamTotalInstalled.GigaBytes:0.00}GB, " +
					$"RamTotalFree={computerSnapshot.RamTotalFree.GigaBytes:0.00}GB";

				Log.Default.Write(LogSeverityType.Information, "Loading font sizes from settings...");

				// User-scoped settings are read from either:
				// ... C:\Users\<UserName>\AppData\Local\Blue_Dot_Brigade\BlueDotBrigade.Weevil.Gui_Url_<HashValue>\2.11.0.0\user.config
				// ... C:\Users\<UserName>\AppData\Local\Weevil\<Version>\user.config
				Application.Current.Resources["ApplicationFontSize"] = Settings.Default.ApplicationFontSize;
				Application.Current.Resources["RowFontSize"] = Settings.Default.RowFontSize;

				Log.Default.Write(
					LogSeverityType.Information,
					computerDetails);
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
			Log.Default.Write(LogSeverityType.Information, "Saving font sizes to settings.");
			
			Settings.Default.ApplicationFontSize = (double)Application.Current.Resources["ApplicationFontSize"];
			Settings.Default.RowFontSize = (double)Application.Current.Resources["RowFontSize"];
			Settings.Default.Save();

			Log.Default.Write(
				LogSeverityType.Information,
				"Weevil application is closing...");
		}
	}
}