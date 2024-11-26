namespace BlueDotBrigade.Weevil
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SidecarDataShould
	{
		[TestMethod]
		public void UseLineNumberWhenLoadingUserComment()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("UseLineNumberWhenLoadingUserComment.log"))
				.Open();

			engine.Selector.Select(31);

			engine.Clear(ClearOperation.BeforeSelected);

			Assert.AreEqual(32, engine.Filter.Results[1].LineNumber);
			Assert.AreEqual("Index31", engine.Filter.Results[1].Metadata.Comment);
		}

		[TestMethod]
		public void KeepCommentsForRecordsNoLongerInMemory()
		{
			IEngine firstEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			Assert.AreEqual("First", firstEngine[0].Metadata.Comment);

			firstEngine.Selector.Select(5);
			firstEngine.Clear(ClearOperation.BeforeSelected);
			firstEngine.Save(true);
			firstEngine = null;

			IEngine secondEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			Assert.AreEqual("First", secondEngine[0].Metadata.Comment);
		}
	}
}
