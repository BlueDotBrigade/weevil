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
	using Cocona;
	using Diagnostics;
	using Filter;
	using Security;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Diagnostics;

	// ReSharper disable once ClassNeverInstantiated.Global
	internal class Program
	{
		private const string BuildConfiguration =
#if DEBUG
			"DEBUG";
#else
			"RELEASE";
#endif

		internal static Version ApplicationVersion { get; } = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);
		internal static bool IsDebuggerAttachedAtStartup { get; } = Debugger.IsAttached;
        internal static string TelemetrySource { get; } = TelemetryClientFactory.GetTelemetrySource();

		public static void Main(string[] args)
		{
			var formatter = OutputAs.ResolveFormatter(args, new MarkdownFormatter());
			OutputWriterContext.Configure(formatter, new ConsoleWriter());

			Log.Default.Write(LogSeverityType.Debug, "Weevil console application has started.");
			Log.Register(new NLogWriter());
			Log.Default.Write(LogSeverityType.Information, $"Build configuration={BuildConfiguration}");
			Log.Default.Write($"Weevil command line parameters. Arguments={Environment.GetCommandLineArgs().Length}");

			var telemetryClient = TelemetryClientFactory.Create();
			TelemetrySessionLifecycle.Shared.Configure(telemetryClient);
			TelemetrySessionLifecycle.Shared.ConfigureStartupContext(TelemetrySource, IsDebuggerAttachedAtStartup);
			Log.Default.Write(LogSeverityType.Debug,
				$"Telemetry client configured. Type={telemetryClient.GetType().Name}");

			var argumentsWithoutOutputAs = OutputAs.RemoveFromArguments(args);
			var builder = CoconaApp.CreateBuilder(argumentsWithoutOutputAs);

			var application = builder.Build();

			application.AddCommands<FilterCommands>();
			application.AddCommands<InsightCommands>();
			application.AddCommands<SecureCommands>();

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
			Log.Default.Write(LogSeverityType.Error, e.Exception.Message);
			Environment.Exit(e.Exception.HResult);
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;

			var message = exception is null
				? "Application is exiting due to an unexpected error."
				: $"Application is exiting due to an unexpected error: {exception?.Message}";

			Log.Default.Write(LogSeverityType.Error, message);

			Environment.Exit(exception?.HResult ?? 1);
		}

	}		
}