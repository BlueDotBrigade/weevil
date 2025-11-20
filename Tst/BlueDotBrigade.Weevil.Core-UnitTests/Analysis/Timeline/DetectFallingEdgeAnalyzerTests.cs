namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class DetectFallingEdgeAnalyzerTests
	{
		private IUserDialog GetUserDialog(string analysisOrder = "Ascending")
		{
			var userDialog = Substitute.For<IUserDialog>();

			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(analysisOrder);

			return userDialog;
		}

		private FilterStrategy CreateMockFilterStrategy(string regexPattern)
		{
			var filterStrategy = Substitute.For<FilterStrategy>();
			var inclusiveFilter = Substitute.For<LogicalOrOperation>();
			var regularExpression = new RegularExpression(regexPattern);

			inclusiveFilter.Count.Returns(1);
			inclusiveFilter.GetRegularExpressions().Returns(ImmutableArray.Create(regularExpression));

			filterStrategy.InclusiveFilter.Returns(inclusiveFilter);

			return filterStrategy;
		}

		[TestMethod]
		public void Analyze_ValuesRiseAndFall_DetectsFallingEdge()
		{
			// Arrange
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			// Create records with pattern: 1 → 2 → 3 → 2
			// The bug would miss the 3 → 2 falling edge because previous value gets stuck at 1
			var records = new List<IRecord>
			{
				new Record(0, now.AddSeconds(0), severity, "Value=1"),
				new Record(1, now.AddSeconds(1), severity, "Value=2"),
				new Record(2, now.AddSeconds(2), severity, "Value=3"),
				new Record(3, now.AddSeconds(3), severity, "Value=2"),
			};

			var filterStrategy = CreateMockFilterStrategy(@"Value=(?<Value>\d+)");
			var analyzer = new DetectFallingEdgeAnalyzer(filterStrategy);

			// Act
			var results = analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(),
				canUpdateMetadata: true);

			// Assert
			// First record (Value=1) should be flagged as initial value
			Assert.IsTrue(records[0].Metadata.IsFlagged, "First record should be flagged");
			
			// Second record (Value=2) should not be flagged (rising edge)
			Assert.IsFalse(records[1].Metadata.IsFlagged, "Second record should not be flagged (rising edge)");
			
			// Third record (Value=3) should not be flagged (rising edge)
			Assert.IsFalse(records[2].Metadata.IsFlagged, "Third record should not be flagged (rising edge)");
			
			// Fourth record (Value=2) MUST be flagged (falling edge from 3 to 2)
			Assert.IsTrue(records[3].Metadata.IsFlagged, "Fourth record should be flagged (falling edge from 3 to 2)");
			
			// Should detect 2 flagged records: initial value + one falling edge
			Assert.AreEqual(2, results.FlaggedRecords);
		}

		[TestMethod]
		public void Analyze_MultipleFallingEdges_DetectsAllFallingEdges()
		{
			// Arrange
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			// Create records with pattern: 10 → 20 → 15 → 25 → 10
			// Should detect falling edges at: 20→15 and 25→10
			var records = new List<IRecord>
			{
				new Record(0, now.AddSeconds(0), severity, "Value=10"),
				new Record(1, now.AddSeconds(1), severity, "Value=20"),
				new Record(2, now.AddSeconds(2), severity, "Value=15"),
				new Record(3, now.AddSeconds(3), severity, "Value=25"),
				new Record(4, now.AddSeconds(4), severity, "Value=10"),
			};

			var filterStrategy = CreateMockFilterStrategy(@"Value=(?<Value>\d+)");
			var analyzer = new DetectFallingEdgeAnalyzer(filterStrategy);

			// Act
			var results = analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(),
				canUpdateMetadata: true);

			// Assert
			Assert.IsTrue(records[0].Metadata.IsFlagged, "First record should be flagged");
			Assert.IsFalse(records[1].Metadata.IsFlagged, "Second record should not be flagged (rising)");
			Assert.IsTrue(records[2].Metadata.IsFlagged, "Third record should be flagged (falling from 20 to 15)");
			Assert.IsFalse(records[3].Metadata.IsFlagged, "Fourth record should not be flagged (rising)");
			Assert.IsTrue(records[4].Metadata.IsFlagged, "Fifth record should be flagged (falling from 25 to 10)");
			
			// Should detect 3 flagged records: initial + two falling edges
			Assert.AreEqual(3, results.FlaggedRecords);
		}

		[TestMethod]
		public void Analyze_OnlyRisingValues_NoFallingEdgesDetected()
		{
			// Arrange
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, now.AddSeconds(0), severity, "Value=1"),
				new Record(1, now.AddSeconds(1), severity, "Value=2"),
				new Record(2, now.AddSeconds(2), severity, "Value=3"),
				new Record(3, now.AddSeconds(3), severity, "Value=4"),
			};

			var filterStrategy = CreateMockFilterStrategy(@"Value=(?<Value>\d+)");
			var analyzer = new DetectFallingEdgeAnalyzer(filterStrategy);

			// Act
			var results = analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(),
				canUpdateMetadata: true);

			// Assert
			// Only the first record should be flagged (as initial value)
			Assert.IsTrue(records[0].Metadata.IsFlagged);
			Assert.IsFalse(records[1].Metadata.IsFlagged);
			Assert.IsFalse(records[2].Metadata.IsFlagged);
			Assert.IsFalse(records[3].Metadata.IsFlagged);
			
			Assert.AreEqual(1, results.FlaggedRecords);
		}
	}
}
