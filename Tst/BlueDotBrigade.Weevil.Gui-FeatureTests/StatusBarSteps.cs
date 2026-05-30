namespace BlueDotBrigade.Weevil.Gui
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[Binding]
	internal class StatusBarSteps : ReqnrollSteps
	{
		internal StatusBarSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[Then($"the status bar visible record count will be {X.WholeNumber}")]
		public void ThenTheStatusBarVisibleRecordCountWillBe(int recordCount)
		{
			this.Context.StatusBar.TotalRecordCount.Should().Be(recordCount);
		}

		[Then($"the status bar bookmark count will be {X.WholeNumber}")]
		public void ThenTheStatusBarBookmarkCountWillBe(int bookmarkCount)
		{
			this.Context.StatusBar.BookmarkDetails.BookmarkCount.Should().Be(bookmarkCount);
		}

		[Then($"the status bar region count will be {X.WholeNumber}")]
		public void ThenTheStatusBarRegionCountWillBe(int regionCount)
		{
			this.Context.StatusBar.RegionDetails.RegionCount.Should().Be(regionCount);
		}
	}
}