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

		[Then($"the visible record count in the status bar will be {X.WholeNumber}")]
		public void ThenTheVisibleRecordCountInTheStatusBarWillBe(int recordCount)
		{
			Assert.AreEqual(
				recordCount,
				this.Context.StatusBar.TotalRecordCount);
		}
	}
}