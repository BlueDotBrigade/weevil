namespace BlueDotBrigade.Weevil.Cli.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Cocona.Command;
	using Cocona.Help;
	using Cocona.Help.DocumentModel;

	internal class FilterCommandHelp : ICoconaHelpTransformer
	{
		public void TransformHelp(HelpMessage helpMessage, CommandDescriptor command)
		{
			helpMessage.Children.Add(
				new HelpSection(
					new HelpHeading("Filter Examples:"),
					new HelpSection(
						new HelpParagraph(@"WeevilCli.exe filter --log-path ""C:\Temp\Http.log"" --Include ""Error 404"""),
						new HelpParagraph(""),
						new HelpParagraph("A log larger than memory:"),
						new HelpParagraph(@"WeevilCli.exe filter --log-path ""C:\Temp\Http.log"" --Include ""Error 404"" --storage Disk"),
						new HelpParagraph(""),
						new HelpParagraph("Only one region, of those the log's sidecar lists:"),
						new HelpParagraph(@"WeevilCli.exe filter --log-path ""C:\Temp\Http.log"" --Include ""Error 404"" --region ""Startup"""),
						new HelpParagraph(""),
						new HelpParagraph("Only one level-1 section, of those the log's sidecar lists, with the sections nested in it:"),
						new HelpParagraph(@"WeevilCli.exe filter --log-path ""C:\Temp\Http.log"" --Include ""Error 404"" --section ""Startup"""),
						new HelpParagraph("")
					)
				));
		}
	}
}