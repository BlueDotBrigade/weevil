namespace BlueDotBrigade.Weevil.Cli
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using Analysis;
	using Diagnostics;
	using Filter;
	using IO;

	// ReSharper disable once ClassNeverInstantiated.Global
	internal class Program
	{
		[SuppressMessage("Microsoft.Usage", "CA1801")]
		[SuppressMessage("Microsoft.Globalization", "CA1303")]
		[SuppressMessage("ReSharper", "UnusedParameter.Local")]
		private static void Main(string[] args)
		{
			var sourceFilePath = string.Empty;
			var inclusiveFilter = string.Empty;

			try
			{
				Log.Default.Write(LogSeverityType.Debug, "Weevil console application is starting...");

				Log.Register(new NLogWriter());

				Log.Default.Write($"Weevil console application has started. Arguments={args.Length}");

				if (args.Length >= 2)
				{
					sourceFilePath = args[0];
					inclusiveFilter = args[1];

					IEngine engine = Engine
						.UsingPath(sourceFilePath)
						.Open();

					engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(inclusiveFilter));

					var destinationFile =
						Path.GetFileNameWithoutExtension(sourceFilePath) +
						".Results" +
						Path.GetExtension(sourceFilePath);

					var destinationFilePath = Path.Combine(
						Path.GetDirectoryName(sourceFilePath),
						destinationFile);

					new ConsoleWriter(FileFormatType.Raw).Write(engine.Filter.Results);
				}
				else
				{
					Console.WriteLine("Usage:");
					Console.WriteLine("\tWeevilCli.exe [source] [inclusive Filter]");
				}
			}
			catch (Exception exception)
			{
				Log.Default.Write(
					LogSeverityType.Critical,
					exception,
					"An unexpected error was encountered while attempting to process the provided log file.",
					new Dictionary<string, object>
					{
							{ "SourceFilePath", sourceFilePath },
					});

				throw;
			}

			System.Console.WriteLine(Resources.PressAnyKeyToExit);
			System.Console.ReadKey();
		}
	}
}