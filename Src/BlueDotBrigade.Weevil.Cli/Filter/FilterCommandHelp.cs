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
					new HelpHeading("Examples:"),
					new HelpSection(
						new HelpParagraph(@"WeevilCli.exe filter --log-path ""C:\Temp\Http.log"" --Include ""Error 404"""),
						new HelpParagraph("")
					)
				));
		}
	}
}