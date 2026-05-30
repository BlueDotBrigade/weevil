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

			engine.Filter.Results[1].LineNumber.Should().Be(32);
			engine.Filter.Results[1].Metadata.Comment.Should().Be("Index31");
		}

		[TestMethod]
		public void KeepCommentsForRecordsNoLongerInMemory()
		{
			IEngine firstEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			firstEngine[0].Metadata.Comment.Should().Be("First");

			firstEngine.Selector.Select(5);
			firstEngine.Clear(ClearOperation.BeforeSelected);
			firstEngine.Save(true);
			firstEngine = null;

			IEngine secondEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			secondEngine[0].Metadata.Comment.Should().Be("First");
		}
	}
}
