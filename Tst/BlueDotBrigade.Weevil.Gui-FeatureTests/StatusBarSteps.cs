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
	}
}