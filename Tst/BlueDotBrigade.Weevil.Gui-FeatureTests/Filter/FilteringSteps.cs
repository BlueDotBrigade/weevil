using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueDotBrigade.Weevil.Gui.Filter
{
	[Binding]
	internal sealed class FilteringSteps : ReqnrollSteps
	{
		private static readonly TimeSpan FilterTimeout = TimeSpan.FromSeconds(10);

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
			FilterAndWait();
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
			FilterAndWait();
		}

		[Then($@"there will be {X.WholeNumber} matching records")]
		public void ThenThereWillBeMatchingRecords(int recordCount)
		{			
			this.Context.StatusBar.FilterDetails.VisibleRecordCount
				.Should()
				.Be(recordCount);
		}

		/// <summary>
		/// Filters, then waits until the results are shown: the filter runs in the background.
		/// </summary>
		private void FilterAndWait()
		{
			RowList rowsBefore = this.Context.Filter.Rows;

			this.Context.Filter.Filter();

			var stopwatch = Stopwatch.StartNew();

			while ((ReferenceEquals(this.Context.Filter.Rows, rowsBefore) || !this.Context.Filter.CanOpenLogFile) &&
				stopwatch.Elapsed < FilterTimeout)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(50));
			}
		}
	}
}