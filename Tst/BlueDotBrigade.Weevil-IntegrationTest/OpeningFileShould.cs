
namespace BlueDotBrigade.Weevil
{
	using System;
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

		[TestMethod]
		public void LoadUsingSidecarContext()
		{
			var fileFormatKey = "FileFormat";

			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("LogWithSidecarContext.log"))
				.Open();

			Assert.AreEqual("1.2.3.4", engine.Context[fileFormatKey]);
		}

		[TestMethod]
		public void LoadUsingExplicitContext()
		{
			var fileFormatKey = "FileFormat";
			var fileFormatValue = new Version(512, 512).ToString();

			var context = new ContextDictionary
			{
				{ fileFormatKey, fileFormatValue},
			};

			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("LogWithSidecarContext.log"))
				.UsingContext(context)
				.Open();

			Assert.AreEqual(fileFormatValue, engine.Context[fileFormatKey]);
		}
	}
}
