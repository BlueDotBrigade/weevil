namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class SelectionManagerTest
	{
		[TestMethod]
		public void Select_FirstRecord_Line1Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);

			RecordList selectedRecords = engine.Selector.GetSelected();

			selectedRecords.Count.Should().Be(1);
			selectedRecords[0].LineNumber.Should().Be(1);
		}
		[TestMethod]
		public void Select_LastRecord_Line32Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 32);

			RecordList selectedRecords = engine.Selector.GetSelected();

			selectedRecords.Count.Should().Be(1);
			selectedRecords[0].LineNumber.Should().Be(32);
		}

		[TestMethod]
		public void Select_NonExistentRecord_ThrowsRecordNotFound()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			Action act = () => engine.Selector.Select(lineNumber: int.MaxValue);
			act.Should().Throw<RecordNotFoundException>();

			engine.Selector.Selected.Count.Should().Be(0);
		}

		[TestMethod]
		public void GivenALineTheFilterHides_WhenSelectedByLineNumber_ThenRecordNotFoundIsThrown()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();
			engine.Filter.Apply(Filter.FilterType.PlainText, new Filter.FilterCriteria("Id=002"));

			Action act = () => engine.Selector.Select(lineNumber: 1);

			act.Should().Throw<RecordNotFoundException>();
			engine.Selector.Selected.Count.Should().Be(0);
		}

		[TestMethod]
		public void GivenALineTheFilterHides_WhenUnselectedByLineNumber_ThenRecordNotFoundIsThrownAndTheSelectionIsKept()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();
			engine.Filter.Apply(Filter.FilterType.PlainText, new Filter.FilterCriteria("Id=002"));
			var shownLineNumber = engine.Filter.Results.GetLineNumber(0);
			engine.Selector.Select(shownLineNumber);

			Action act = () => engine.Selector.Unselect(lineNumber: 1);

			act.Should().Throw<RecordNotFoundException>();
			engine.Selector.Selected.Keys.Should().Equal(shownLineNumber);
		}

		[TestMethod]
		public void GivenLinesTheFilterHides_WhenSelectedOnly_ThenOnlyTheRecordsShownBetweenThemAreSelected()
		{
			RecordList records = R.WithLineNumbers(1, 10);
			RecordList odd = records.Where(r => r.LineNumber % 2 == 1).ToImmutableArray();
			SelectionManager selector = CreateSelector(records, odd);

			selector.SelectOnly(2, 6);

			selector.Selected.Keys.Should().Equal(3, 5);
		}

		[TestMethod]
		public void GivenLinesBeyondTheRecords_WhenSelectedOnly_ThenNothingIsSelected()
		{
			RecordList records = R.WithLineNumbers(1, 10);
			SelectionManager selector = CreateSelector(records);
			selector.SelectOnly(1, 3);

			selector.SelectOnly(100, 200);

			selector.Selected.Count.Should().Be(0);
		}

		[TestMethod]
		public void GivenALineTheFilterHides_WhenToggled_ThenRecordNotFoundIsThrown()
		{
			RecordList records = R.WithLineNumbers(1, 10);
			RecordList odd = records.Where(r => r.LineNumber % 2 == 1).ToImmutableArray();
			SelectionManager selector = CreateSelector(records, odd);

			Action toggle = () => selector.Toggle(2);

			toggle.Should().Throw<RecordNotFoundException>();
			selector.Selected.Count.Should().Be(0);
		}

		private static SelectionManager CreateSelector(RecordList records)
		{
			return CreateSelector(records, records);
		}

		private static SelectionManager CreateSelector(RecordList records, RecordList visibleRecords)
		{
			var state = new RecordState();
			var navigationManager = new NavigationManager(null, null, visibleRecords, null, state);

			return new SelectionManager(new RecordScope(records), visibleRecords, navigationManager, Encoding.UTF8, state, () => { });
		}
	}
}
