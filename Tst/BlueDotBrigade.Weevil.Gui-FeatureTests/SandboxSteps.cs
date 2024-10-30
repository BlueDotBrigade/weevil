namespace BlueDotBrigade.Weevil.Gui
{
	using BlueDotBrigade.DatenLokator.TestsTools;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[Binding]
	internal class SandBoxSteps : ReqnrollSteps
	{
		internal SandBoxSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[Then("exception thrown when reading ViewModel property via the token")]
		public void ThenExceptionThrownWhenReadingViewModelPropertyViaTheToken()
		{
			// InvalidOperationException: The calling thread cannot access this object because a different thread owns it.
			Assert.AreEqual(
				387,
				this.Context.StatusBar.TotalRecordCount);
		}
	}
}
