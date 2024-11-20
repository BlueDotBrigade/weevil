using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueDotBrigade.Weevil.Gui.Filter
{
	[Binding]
	internal sealed class FilteringSteps : ReqnrollSteps
	{
		public FilteringSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"applying the include filter: {X.AnyText}")]
		public void WhenApplyingTheIncludeFilter(string includeFilter)
		{
			this.Context.Filter.InclusiveFilter = includeFilter;
			this.Context.Filter.Filter();
		}
	}
}