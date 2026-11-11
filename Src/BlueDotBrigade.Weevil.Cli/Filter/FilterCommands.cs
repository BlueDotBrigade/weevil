namespace BlueDotBrigade.Weevil.Cli.Filter
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Test;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(FilterCommandHelp))]
	internal class FilterCommands
	{
		[Command(
			name: "Filter",
			Aliases = new[] { "f" },
			Description = "Filters the log file for records that include/exclude the specified text.")]
		public void Filter(
			string logPath,
			string? include,
			string? exclude,
			[Option(Description = "Ram keeps the log in memory. Disk reads it back from the log, for a log larger than memory.")]
			StorageMode storage = StorageMode.Ram)
		{
			var telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliFilterCommand);

				IEngine engine = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.UsingStorage(storage)
						.Open();

				var destinationFile =
					Path.GetFileNameWithoutExtension(logPath) +
					".Results" +
					Path.GetExtension(logPath);

				var destinationFilePath = Path.Combine(
					Path.GetDirectoryName(logPath),
					destinationFile);

				Encoding encoding;
				using (var log = FileHelper.Open(logPath))
				{
					encoding = EncodingHelper.GetEncoding(log);
				}

				// On disk, a log that changed while it was read is reloaded, then filtered and saved once more (#1028).
				engine.RunReloadingOnChange(current =>
				{
					current.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(include, exclude));
					SaveResults(current, destinationFilePath, encoding);
					return true;
				});
			}
			finally
			{
				telemetry.EndSession();
			}
		}

		/// <summary>
		/// Saves the results as the GUI saves records in raw format: one record per line, in the log's encoding (#1001).
		/// </summary>
		/// <remarks>
		/// Written to a temporary file beside the destination, which replaces it once every record is written,
		/// so a save that fails, for example because the log changed, leaves no partial file (#1028).
		/// </remarks>
		private static void SaveResults(IEngine engine, string destinationFilePath, Encoding encoding)
		{
			var temporaryFilePath = $"{destinationFilePath}.{Guid.NewGuid():N}.tmp";

			try
			{
				// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
				var buffer = new ContentBuffer();
				System.IO.File.WriteAllLines(
					temporaryFilePath,
					engine.Filter.Results.Select(record => buffer.Read(record).ToString()),
					encoding);

				System.IO.File.Move(temporaryFilePath, destinationFilePath, overwrite: true);
			}
			finally
			{
				// Gone once moved into place.
				System.IO.File.Delete(temporaryFilePath);
			}
		}
	}
}
