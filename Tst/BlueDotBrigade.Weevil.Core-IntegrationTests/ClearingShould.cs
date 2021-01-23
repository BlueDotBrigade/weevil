namespace BlueDotBrigade.Weevil
{
	using Data;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Weevil.Collections.Immutable;

	[TestClass]
	public class ClearingShould
	{
		[TestMethod]
		public void SupportClearingSelectedRecords()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearRecordsOperation.Selected);

			Assert.AreEqual(512 - 1, engine.Count);
			Assert.AreEqual(1, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(512, engine.Filter.Results.Last().LineNumber);
		}

		[TestMethod]
		public void SupportClearingUnselectedRecords()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearRecordsOperation.Unselected);

			Assert.AreEqual(1, engine.Count);
			Assert.AreEqual(9, engine.Filter.Results.First().LineNumber);
		}

		[TestMethod]
		public void SupportClearingBeforeFirstRecord()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 1);

			engine.Clear(ClearRecordsOperation.BeforeSelected);

			Assert.AreEqual(1, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(512, engine.Filter.Results.Last().LineNumber);
		}

		[TestMethod]
		public void SupportClearingAfterLastRecord()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 32);

			engine.Clear(ClearRecordsOperation.AfterSelected);

			Assert.AreEqual(32, engine.Count);
			Assert.AreEqual(1, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(32, engine.Filter.Results.Last().LineNumber);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeSelection()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearRecordsOperation.BeforeSelected);

			Assert.AreEqual(512 - 8, engine.Count);
			Assert.AreEqual(9, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(512, engine.Filter.Results.Last().LineNumber);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeAndAfterSelection()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 200); // Start of Section 200

			engine
				.Selector
				.Select(lineNumber: 400); // Start of Section 400

			engine.Clear(ClearRecordsOperation.BeforeAndAfterSelected);

			Assert.AreEqual(201, engine.Count);
			Assert.AreEqual(200, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(400, engine.Filter.Results.Last().LineNumber);
		}

		[TestMethod]
		public void SupportClearingRecordsAfterSelection()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearRecordsOperation.AfterSelected);

			Assert.AreEqual(9, engine.Count);
			Assert.AreEqual(1, engine.Filter.Results.First().LineNumber);
			Assert.AreEqual(9, engine.Filter.Results.Last().LineNumber);
		}
	}
}
