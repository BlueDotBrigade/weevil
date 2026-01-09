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
			Assert.AreEqual(
				recordCount,
				this.Context.StatusBar.TotalRecordCount);
		}

		[Then($"the status bar bookmark count will be {X.WholeNumber}")]
		public void ThenTheStatusBarBookmarkCountWillBe(int bookmarkCount)
		{
			Assert.AreEqual(
				bookmarkCount,
				this.Context.StatusBar.FilterDetails.BookmarkCount);
		}

		[Then($"the status bar region count will be {X.WholeNumber}")]
		public void ThenTheStatusBarRegionCountWillBe(int regionCount)
		{
			Assert.AreEqual(
				regionCount,
				this.Context.StatusBar.FilterDetails.RegionCount);
		}
	}
}