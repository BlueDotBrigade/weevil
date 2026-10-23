namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
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

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			selectedRecords.Length.Should().Be(1);
			selectedRecords[0].LineNumber.Should().Be(1);
		}
		[TestMethod]
		public void Select_LastRecord_Line32Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 32);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			selectedRecords.Length.Should().Be(1);
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
		public void GivenEveryOtherRecordSelected_WhenTheOthersAreSelectedToo_ThenItTakesLessThanFiveSeconds()
		{
			// Regression: the #1055 review. Changing one record at a time took about 30 seconds here.
			ImmutableArray<IRecord> records = R.WithLineNumbers(1, 300_000);
			SelectionManager selector = CreateSelector(records);
			selector.Select(records.Where(r => r.LineNumber % 2 == 1).ToList());
			List<IRecord> others = records.Where(r => r.LineNumber % 2 == 0).ToList();

			var stopwatch = Stopwatch.StartNew();
			selector.Select(others);

			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
			selector.Selected.Count.Should().Be(records.Length);
		}

		[TestMethod]
		public void GivenEveryOtherRecordSelected_WhenTheyAreUnselected_ThenItTakesLessThanFiveSeconds()
		{
			// Regression: the #1055 review. Changing one record at a time took about 30 seconds here.
			ImmutableArray<IRecord> records = R.WithLineNumbers(1, 300_000);
			SelectionManager selector = CreateSelector(records);
			List<IRecord> scattered = records.Where(r => r.LineNumber % 2 == 1).ToList();
			selector.Select(scattered);

			var stopwatch = Stopwatch.StartNew();
			selector.Unselect(scattered);

			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
			selector.Selected.Count.Should().Be(0);
		}

		private static SelectionManager CreateSelector(ImmutableArray<IRecord> records)
		{
			return new SelectionManager(records, records, new NavigationManager(null, null, records, null), Encoding.UTF8, () => { });
		}
	}
}
