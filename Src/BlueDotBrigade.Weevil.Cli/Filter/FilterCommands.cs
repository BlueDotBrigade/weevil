namespace BlueDotBrigade.Weevil.Cli.Filter
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
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
			IEngine engine = Engine
					.UsingPath(logPath)
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
	}
}