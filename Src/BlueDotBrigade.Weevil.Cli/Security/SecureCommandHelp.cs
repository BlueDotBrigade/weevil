namespace BlueDotBrigade.Weevil.Cli.Security
{
	using Cocona.Command;
	using Cocona.Help;
	using Cocona.Help.DocumentModel;

	internal class SecureCommandHelp : ICoconaHelpTransformer
	{
		public void TransformHelp(HelpMessage helpMessage, CommandDescriptor command)
		{
			helpMessage.Children.Add(
				new HelpSection(
					new HelpHeading("Examples:"),
					new HelpSection(
						new HelpParagraph(@"WeevilCli.exe protect-secret --secret ""MyP@ssword"""),
						new HelpParagraph("")
					)
				));
		}
	}
}
