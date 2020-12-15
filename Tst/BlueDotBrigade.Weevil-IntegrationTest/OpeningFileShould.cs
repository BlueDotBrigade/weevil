
namespace BlueDotBrigade.Weevil
{
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OpeningFileShould
	{
		[TestMethod]
		public void OpenEmptyFile()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("EmptyFile.txt"))
				.Open();

			Assert.AreEqual(0, engine.Count);
		}

		[TestMethod]
		public void OpenFileWithOnlyWhitespace()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("FileWithOnlyWhitespace.txt"))
				.Open();

			Assert.AreEqual(1, engine.Count);
		}

		[TestMethod]
		public void LoadStartingAtLineNumber()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"), lineNumber: 100)
				.Open();

			Assert.AreEqual(100, engine[0].LineNumber);
			Assert.IsTrue(engine[0].Content.Contains("Section100"));
		}
	}
}
