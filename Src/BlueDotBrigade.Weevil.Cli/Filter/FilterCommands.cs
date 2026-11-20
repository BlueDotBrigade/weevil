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
			StorageMode storage = StorageMode.Ram,
			[Option(Description = "Filters only this region, one of those the log's sidecar lists.")]
			string? region = null,
			[Option(Description = "Filters only this level-1 section, one of those the log's sidecar lists, with the sections nested in it.")]
			string? section = null)
		{
			TelemetrySessionLifecycle telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliFilterCommand);

				IEngineBuilder builder = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.UsingStorage(storage);

				// Only the region's or the section's records are opened (#1213, #1212).
				builder = region is null ? builder : builder.UsingRegion(region);
				builder = section is null ? builder : builder.UsingSection(section);

				// Before the log opens, so a log renamed meanwhile does not stop a save that has every record it needs (#1247).
				Encoding encoding;
				using (FileStream log = FileHelper.Open(logPath))
				{
					encoding = EncodingHelper.GetEncoding(log);
				}

				// Closed when the command ends, so another Weevil can open the log (#1254).
				using IEngine engine = builder.Open();

				var destinationFile =
					Path.GetFileNameWithoutExtension(logPath) +
					".Results" +
					Path.GetExtension(logPath);

				var destinationFilePath = Path.Combine(
					Path.GetDirectoryName(logPath),
					destinationFile);

				// A log renamed, deleted or replaced meanwhile is not reloaded: the analyst runs the command again (#1247).
				engine.RunOnce(
					() =>
					{
						engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(include, exclude));
						SaveResults(engine, destinationFilePath, encoding);
						return true;
					},
					Program.ReportWarning);
			}
			catch (Exception exception) when (exception is LogFileChangedException or LogInUseException or NotSupportedException)
			{
				// A log changed while it was read (#1247), one another Weevil has open (#1254), or one Weevil can't open, such as one with too many lines (#1252):
				// its message, without a stack trace.
				// Cocona sets the exit code from the command, so it is passed this way.
				throw new CommandExitedException(exception.Message, 1);
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
			var isSaved = false;

			try
			{
				// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
				var buffer = new ContentBuffer();
				System.IO.File.WriteAllLines(
					temporaryFilePath,
					engine.Filter.Results.Select(record => buffer.Read(record).ToString()),
					encoding);

				System.IO.File.Move(temporaryFilePath, destinationFilePath, overwrite: true);
				isSaved = true;
			}
			finally
			{
				if (!isSaved)
				{
					DeleteTemporaryFile(temporaryFilePath);
				}
			}
		}

		/// <summary>
		/// Deletes the records written before a save stopped, without hiding why it stopped: a change to the log must still reach the caller (#1247).
		/// </summary>
		private static void DeleteTemporaryFile(string temporaryFilePath)
		{
			try
			{
				System.IO.File.Delete(temporaryFilePath);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Warning, $"A temporary file could not be deleted. Path={temporaryFilePath}, Reason=`{exception.Message}`");
			}
		}
	}
}
