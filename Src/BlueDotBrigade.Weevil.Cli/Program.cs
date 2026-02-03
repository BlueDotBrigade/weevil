namespace BlueDotBrigade.Weevil.Cli
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Threading.Tasks;
	using Analysis;
	using Cocona;
	using Diagnostics;
	using Filter;
	using IO;
	using BlueDotBrigade.Weevil.IO;

	// ReSharper disable once ClassNeverInstantiated.Global
	internal class Program
	{
		public static void Main()
		{
			OutputWriterContext.Configure(new MarkdownFormatter(), new ConsoleWriter());

			Log.Default.Write(LogSeverityType.Debug, "Weevil console application has started.");
			Log.Register(new NLogWriter());
			Log.Default.Write($"Weevil console application is initializing... Arguments={Environment.GetCommandLineArgs().Length}");

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
