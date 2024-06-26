namespace BlueDotBrigade.Weevil.Cli.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Cocona.Command;
	using Cocona.Help;
	using Cocona.Help.DocumentModel;

	internal class InsightCommandHelp : ICoconaHelpTransformer
	{
		public void TransformHelp(HelpMessage helpMessage, CommandDescriptor command)
		{
			helpMessage.Children.Add(
				new HelpSection(
					new HelpHeading("Examples:"),
					new HelpSection(
						new HelpParagraph(@"WeevilCli.exe insight --log-path ""C:\Temp\Http.log"""),
						new HelpParagraph("")
					)
				));
		}
	}
}