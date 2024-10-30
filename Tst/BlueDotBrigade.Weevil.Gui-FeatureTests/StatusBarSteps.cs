namespace BlueDotBrigade.Weevil.Gui
{
	using BlueDotBrigade.DatenLokator.TestsTools;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[Binding]
	internal class StatusBarSteps : ReqnrollSteps
	{
		internal StatusBarSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[Then($"the visible record count in the status bar will be {X.Integer}")]
		public void ThenTheVisibleRecordCountInTheStatusBarWillBe(int recordCount)
		{
			Assert.AreEqual(
				recordCount,
				this.Context.StatusBar.TotalRecordCount);
		}
	}
}