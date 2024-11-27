using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueDotBrigade.Weevil.Gui.Filter
{
	[Binding]
	internal sealed class FilterOptionSteps : ReqnrollSteps
	{
		public FilterOptionSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"the automatic filtering option is {X.OnOff}")]
		public void WhenTheAutomaticFilteringOptionIsOn(bool isOn)
		{
			this.Context.Filter.IsManualFilter = !isOn;
		}
	}
}