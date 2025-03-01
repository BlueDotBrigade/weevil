namespace BlueDotBrigade.Weevil
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SourceFileRemarksShould
	{
		[TestMethod]
		public void BeReadWhenFileOpened()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			Assert.AreEqual("These are file level comments.", engine.SourceFileRemarks);
		}
	}
}
