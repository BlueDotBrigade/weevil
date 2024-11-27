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

		[When($"entering the include filter: {X.AnyText}")]
		public void WhenEnteringTheIncludeFilter(string include)
		{
			this.Context.Filter.InclusiveFilter = include;
		}

		[When($"applying the include filter: {X.AnyText}")]
		public void WhenApplyingTheIncludeFilter(string include)
		{
			this.Context.Filter.InclusiveFilter = include;
			this.Context.Filter.Filter();
		}

		[When($"entering the exclude filter: {X.AnyText}")]
		public void WhenEnteringTheExcludeFilter(string exclude)
		{
			this.Context.Filter.ExclusiveFilter = exclude;
		}

		[When($"applying the exclude filter: {X.AnyText}")]
		public void WhenApplyingTheExcludeFilter(string exclude)
		{
			this.Context.Filter.ExclusiveFilter = exclude;
			this.Context.Filter.Filter();
		}

		[Then($@"there will be {X.WholeNumber} matching records")]
		public void ThenThereWillBeMatchingRecords(int recordCount)
		{			
			this.Context.StatusBar.FilterDetails.VisibleRecordCount
				.Should()
				.Be(recordCount);
		}
	}
}