namespace BlueDotBrigade.Weevil.Cli.Filter
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(FilterCommandHelp))]
	internal class FilterCommands
	{
		[Command(
			name: "Filter",
			Aliases = new[] { "f" },
			Description = "Filters the log file for records that include/exclude the specified text.")]
		public void Filter(string logPath, string? include, string? exclude)
		{
			var telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliFilterCommand);

				IEngine engine = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.Open();

				engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(include, exclude));

				var destinationFile =
					Path.GetFileNameWithoutExtension(logPath) +
					".Results" +
					Path.GetExtension(logPath);

				var destinationFilePath = Path.Combine(
					Path.GetDirectoryName(logPath),
					destinationFile);
			}
			finally
			{
				telemetry.EndSession();
			}
		}
	}
}
