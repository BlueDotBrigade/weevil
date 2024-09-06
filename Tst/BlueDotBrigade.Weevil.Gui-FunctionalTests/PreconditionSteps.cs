namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[Binding]
	internal class PreconditionSteps : ReqnrollSteps
	{
		public PreconditionSteps(ScenarioContext scenario) : base(scenario)
		{
		}

		[Given("that the default log file is open")]
		public void GivenThatTheDefaultLogFileIsOpenTwo()
		{
			//await this.Initialize();
			this.Initialize();
		}
	}
}