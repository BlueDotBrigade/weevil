namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Linq;
	using Data;
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ClearingShould
	{
		[TestMethod]
		public void SupportClearingSelectedRecords()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.Selected);

			engine.Count.Should().Be(512 - 1);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingUnselectedRecords()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.Unselected);

			engine.Count.Should().Be(1);
			(engine.Filter.Results.First().LineNumber).Should().Be(9);
		}

		[TestMethod]
		public void SupportClearingBeforeFirstRecord()
		{
			using IEngine engine = Engine
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
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 32);

			engine.Clear(ClearOperation.AfterSelected);

			engine.Count.Should().Be(32);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(32);
		}

		[TestMethod]
		public void SupportClearingBetweenSelectedRecords()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Selector
				.Select(2)
				.Select(511);
			
			engine.Clear(ClearOperation.BetweenSelected);

			engine.Count.Should().Be(4);
			engine.Filter.Results[0].LineNumber.Should().Be(1);
			engine.Filter.Results[1].LineNumber.Should().Be(2);
			engine.Filter.Results[2].LineNumber.Should().Be(511);
			engine.Filter.Results[3].LineNumber.Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeSelection()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.BeforeSelected);

			engine.Count.Should().Be(512 - 8);
			(engine.Filter.Results.First().LineNumber).Should().Be(9);
			(engine.Filter.Results.Last().LineNumber).Should().Be(512);
		}

		[TestMethod]
		public void SupportClearingRecordsBeforeAndAfterSelection()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 200); // Start of Section 200

			engine
				.Selector
				.Select(lineNumber: 400); // Start of Section 400

			engine.Clear(ClearOperation.BeforeAndAfterSelected);

			engine.Count.Should().Be(201);
			(engine.Filter.Results.First().LineNumber).Should().Be(200);
			(engine.Filter.Results.Last().LineNumber).Should().Be(400);
		}

		[TestMethod]
		public void SupportClearingRecordsAfterSelection()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.Clear(ClearOperation.AfterSelected);

			engine.Count.Should().Be(9);
			(engine.Filter.Results.First().LineNumber).Should().Be(1);
			(engine.Filter.Results.Last().LineNumber).Should().Be(9);
		}

		[TestMethod]
		public void GivenRecordsSelectedTogetherAndApart_WhenTheOthersAreClearedAndTheRestFiltered_ThenTheFilterReadsTheRecordsKept()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			foreach (var lineNumber in new[] { 2, 3, 4, 9, 200, 500 })
			{
				engine.Selector.Select(lineNumber);
			}

			engine.Clear(ClearOperation.Unselected);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Changing state"));

			engine.Filter.Results.Select(record => record.LineNumber).Should().Equal(200, 500);
			engine.Filter.Results[1].Content.Should().Contain("new state Section500.");

			engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);

			engine.Filter.Results.Select(record => record.LineNumber).Should().Equal(2, 3, 4, 9, 200, 500);
			engine.Filter.Results[0].Content.Should().Contain("Id=002");
		}

		[TestMethod]
		public void FlagWhenClearOperationHasBeenPerformed()
		{
			using IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine
				.Selector
				.Select(lineNumber: 9);

			engine.HasBeenCleared.Should().BeFalse();
			engine.Clear(ClearOperation.AfterSelected);
			engine.HasBeenCleared.Should().BeTrue();
		}

	}
}
