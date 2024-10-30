namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.TestingTools;
	using BlueDotBrigade.DatenLokator.TestsTools;

	[Binding]
	internal class PreconditionSteps : ReqnrollSteps
	{
		public PreconditionSteps(Token token) : base(token)
		{
		}

		[Given(@"that the default log file is open")]
		public async Task GivenThatTheDefaultLogFileIsOpen()
		{
			var logFilePath = new Daten().AsFilePath(From.GlobalDefault);

			await this.Context.OpenFile(logFilePath);
		}
	}
}