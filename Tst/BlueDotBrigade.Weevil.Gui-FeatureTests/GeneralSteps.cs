namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[Binding]
	internal class GeneralSteps : ReqnrollSteps
	{
		internal GeneralSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"waiting {X.TimePeriod}")]
		public void WhenWaitingFor(TimeSpan waitPeriod)
		{
			base.LogWriter.Info("Waiting {0} for: {1}", waitPeriod, "no reason provided");
			Thread.Sleep(waitPeriod);
		}

		[When($"waiting {X.TimePeriod} for: {X.AnyText}")]
		public void WhenWaitingFor(TimeSpan waitPeriod, string reason)
		{
			base.LogWriter.Info("Waiting {0} for: {1}", waitPeriod, reason);
			Thread.Sleep(waitPeriod);
		}
	}
}
