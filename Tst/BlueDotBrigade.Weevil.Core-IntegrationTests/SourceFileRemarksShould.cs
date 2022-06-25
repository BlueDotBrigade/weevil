namespace BlueDotBrigade.Weevil
{
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SourceFileRemarksShould
	{
		[TestMethod]
		public void BeReadWhenFileOpened()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			Assert.AreEqual("These are file level comments.", engine.SourceFileRemarks);
		}
	}
}
