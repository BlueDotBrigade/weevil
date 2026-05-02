namespace BlueDotBrigade.Weevil.Cli
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Reflection;
	using System.Threading.Tasks;
	using Analysis;
	using BlueDotBrigade.Weevil.Configuration;
	using BlueDotBrigade.Weevil.Data.SqlClient;
	using Cocona;
	using Diagnostics;
	using Filter;
	using IO;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Diagnostics;

	// ReSharper disable once ClassNeverInstantiated.Global
	internal class Program
	{
		internal static Version ApplicationVersion { get; } = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);

		public static void Main()
		{
			OutputWriterContext.Configure(new MarkdownFormatter(), new ConsoleWriter());

			Log.Default.Write(LogSeverityType.Debug, "Weevil console application has started.");
			Log.Register(new NLogWriter());
			Log.Default.Write($"Weevil console application is initializing... Arguments={Environment.GetCommandLineArgs().Length}");

			var isTelemetryEnabled = TelemetryConfiguration.IsEnabled();
			Log.Default.Write(LogSeverityType.Information, $"Telemetry enabled: {isTelemetryEnabled}");

			var telemetryClient = BuildTelemetryClient(isTelemetryEnabled);
			TelemetrySessionLifecycle.Shared.Configure(telemetryClient);
			Log.Default.Write(LogSeverityType.Debug,
				$"Telemetry client configured. Type={telemetryClient.GetType().Name}");

			var builder = CoconaApp.CreateBuilder();

			var application = builder.Build();

			application.AddCommands<FilterCommands>();
			application.AddCommands<InsightCommands>();

			application.Run();

			Log.Default.Write(LogSeverityType.Debug, "Weevil console application is terminating...");
		}


		static Program()
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnhandledTaskException;
		}

		private static void OnUnhandledTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			Write.Error(e.Exception.Message);
			Environment.Exit(e.Exception.HResult);
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;

			var message = exception is null
				? "Application is exiting due to an unexpected error."
				: $"Application is exiting due to an unexpected error: {exception?.Message}";

			Write.Error(message);

			Environment.Exit(exception?.HResult ?? 1);
		}

		private static ITelemetryClient BuildTelemetryClient(bool isTelemetryEnabled)
		{
			if (!isTelemetryEnabled)
			{
				return NullTelemetryClient.Instance;
			}

			var connectionString = TelemetryConfiguration.GetConnectionString();

			return new MsSqlTelemetryClient(new MsSqlTelemetryClientOptions
			{
				ConnectionString = connectionString,
			});
		}
	}		
}
