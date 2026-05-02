namespace BlueDotBrigade.Weevil.Cli
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Reflection;
	using System.Threading.Tasks;
	using Analysis;
	using BlueDotBrigade.Weevil.Configuration;
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
		internal static bool IsDebuggerAttachedAtStartup { get; } = Debugger.IsAttached;
		internal static string TelemetrySource { get; } = TelemetryConfiguration.GetSource();

		public static void Main()
		{
			OutputWriterContext.Configure(new MarkdownFormatter(), new ConsoleWriter());

			Log.Default.Write(LogSeverityType.Debug, "Weevil console application has started.");
			Log.Register(new NLogWriter());
			Log.Default.Write($"Weevil console application is initializing... Arguments={Environment.GetCommandLineArgs().Length}");

			var telemetryClient = TelemetryClientFactory.Create();
			TelemetrySessionLifecycle.Shared.Configure(telemetryClient);
			TelemetrySessionLifecycle.Shared.ConfigureStartupContext(TelemetrySource, IsDebuggerAttachedAtStartup);
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

	}		
}
