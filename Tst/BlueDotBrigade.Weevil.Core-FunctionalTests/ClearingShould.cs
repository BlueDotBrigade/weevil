namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ClearingShould
	{
		[TestMethod]
		public void SupportClearingSelectedRecords()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.Selected);

			(engine.Count).Should().Be(512 - 1);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingUnselectedRecords()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.Unselected);

			(engine.Count).Should().Be(1);
			(engine.Filter.Results.First().LineNumber).Should().Be(9);
		}

		[TestMethod]
		public void SupportClearingBeforeFirstRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 1);

			engine.Clear(ClearOperation.BeforeSelected);

			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingAfterLastRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 32);

			engine.Clear(ClearOperation.AfterSelected);

			(engine.Count).Should().Be(32);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(32);
		}

		[TestMethod]
		public void SupportClearingBetweenSelectedRecords()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			var selection = new List<IRecord>
			{
				engine.Records.RecordAtLineNumber(2),
				engine.Records.RecordAtLineNumber(511),
			};

			engine.Selector.Select(selection);
			
			engine.Clear(ClearOperation.BetweenSelected);

			(engine.Count).Should().Be(4);
			(engine.Filter.Results[0].LineNumber).Should().Be(1);
			(engine.Filter.Results[1].LineNumber).Should().Be(2);
			(engine.Filter.Results[2].LineNumber).Should().Be(511);
			(engine.Filter.Results[3].LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeSelection()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.BeforeSelected);

			(engine.Count).Should().Be(512 - 8);
			(engine.Filter.Results.First().LineNumber).Should().Be(9);
			(engine.Filter.Results.Last().LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeAndAfterSelection()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 200); // Start of Section 200

			engine
				.Selector
				.Select(lineNumber: 400); // Start of Section 400

			engine.Clear(ClearOperation.BeforeAndAfterSelected);

			(engine.Count).Should().Be(201);
			(engine.Filter.Results.First().LineNumber).Should().Be(200);
			(engine.Filter.Results.Last().LineNumber).Should().Be(400);
		}

		[TestMethod]
		public void SupportClearingRecordsAfterSelection()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.AfterSelected);

			(engine.Count).Should().Be(9);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(9);
		}

		[TestMethod]
		public void FlagWhenClearOperationHasBeenPerformed()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			(engine.HasBeenCleared).Should().BeFalse();
			engine.Clear(ClearOperation.AfterSelected);
			(engine.HasBeenCleared).Should().BeTrue();
		}
	}
}
