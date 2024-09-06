
namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using BlueDotBrigade.Weevil.Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OpeningFileShould
	{
		[TestMethod]
		public void OpenEmptyFile()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("EmptyFile.txt"))
				.Open();

			Assert.AreEqual(0, engine.Count);
		}

		[TestMethod]
		public void OpenFileWithOnlyWhitespace()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("FileWithOnlyWhitespace.txt"))
				.Open();

			Assert.AreEqual(1, engine.Count);
		}

		[TestMethod]
		public void LoadStartingAtLineNumber()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault), startAtLineNumber: 100)
				.Open();

			Assert.AreEqual(100, engine[0].LineNumber);
			Assert.IsTrue(engine[0].Content.Contains("Section100"));
		}

		[TestMethod]
		public void LoadUsingSidecarContext()
		{
			var fileFormatKey = "FileFormat";

			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("LogWithSidecarContext.log"))
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
				.UsingPath(new Daten().AsFilePath("LogWithSidecarContext.log"))
				.UsingContext(context)
				.Open();

			Assert.AreEqual(fileFormatValue, engine.Context[fileFormatKey]);
		}

		[TestMethod]
		public void DeleteMe(string logFilePath, string includeFilter, string excludeFilter)
		{
			IEngine engine = Engine
				.UsingPath(logFilePath)
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(includeFilter, excludeFilter));

			foreach (var record in engine.Filter.Results)
			{
				Console.WriteLine(record.ToString());
			}
		}

		//[TestMethod]
		//public void DeleteMe(string logFilePath, string includeFilter, string excludeFilter)
		//{
		//	var context = new ContextDictionary
		//	{
		//		{ "FileFormat", "2.12.0"},
		//	};

		//	IEngine engine = Engine
		//		.UsingPath(logFilePath)
		//		.UsingContext(context)
		//		.Open();

		//	engine.Filter.Apply(
		//		Filter.FilterType.RegularExpression,
		//		new FilterCriteria(includeFilter, excludeFilter));

		//	foreach(var record in engine.Filter.Results)
		//	{
		//		Console.WriteLine(record.ToString());
		//	}
		//}
	}
}