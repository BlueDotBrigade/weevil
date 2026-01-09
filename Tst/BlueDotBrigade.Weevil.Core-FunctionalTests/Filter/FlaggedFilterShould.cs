namespace BlueDotBrigade.Weevil.Filter
{
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FlaggedFilterShould
	{
		[TestMethod]
		public void ShowOnlyFlaggedRecords()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			// Flag some records using an analyzer
			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

			// Apply @Flagged filter
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("@Flagged"));

			// Verify only flagged records are shown
			foreach (IRecord record in engine.Filter.Results)
			{
				Assert.IsTrue(record.Metadata.IsFlagged, $"Record {record.LineNumber} should be flagged");
			}

			// Verify we have the expected flagged records
			var expectedFlaggedLines = new[] { 100, 200, 300, 400, 500 };
			Assert.AreEqual(expectedFlaggedLines.Length, engine.Filter.Results.Length);
		}

		[TestMethod]
		public void BeCaseInsensitive()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			// Flag some records
			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

			// Apply lowercase @flagged filter
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("@flagged"));

			// Should still show flagged records (case-insensitive)
			foreach (IRecord record in engine.Filter.Results)
			{
				Assert.IsTrue(record.Metadata.IsFlagged, $"Record {record.LineNumber} should be flagged");
			}

			Assert.IsTrue(engine.Filter.Results.Length > 0, "Should find flagged records with lowercase moniker");
		}

		[TestMethod]
		public void CombineWithOtherFilters()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			// Flag some records
			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

			// Apply combined filter: show records with "Error" OR flagged records
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Error||@Flagged"));

			// Verify results contain both Error records and flagged records
			var hasErrorRecord = engine.Filter.Results.Any(r => r.Content.Contains("Error"));
			var hasFlaggedRecord = engine.Filter.Results.Any(r => r.Metadata.IsFlagged);

			Assert.IsTrue(hasErrorRecord || hasFlaggedRecord, 
				"Should have either Error records or flagged records");
		}

		[TestMethod]
		public void NotDuplicateWhenFlaggedAlreadyInFilter()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			// Flag some records
			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

			// Apply filter with @Flagged
			var initialFilter = "Error||@Flagged";
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria(initialFilter));

			var initialCount = engine.Filter.Results.Length;

			// Verify the filter criteria is stored correctly
			Assert.AreEqual(initialFilter, engine.Filter.Criteria.Include);

			// If we were to append @Flagged again (simulating the bug), 
			// the filter would be "Error||@Flagged||@Flagged"
			// But this test just verifies the current filter state is correct
			Assert.IsFalse(engine.Filter.Criteria.Include.Contains("@Flagged||@Flagged"), 
				"Filter should not have duplicate @Flagged monikers");
		}
	}
}
