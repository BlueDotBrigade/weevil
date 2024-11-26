namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System.Collections.Immutable;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using global::BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	namespace BlueDotBrigade.Weevil.Common.Analysis.Tests
	{
		[TestClass]
		public class TemporalAnomalyAnalyzerTests
		{
			[TestMethod]
			public void Analyze_NotInChronologicalOrder_CommentUpdated()
			{
				ImmutableArray<IRecord> records = R.Create()
					.WithCreatedAt(0, "10:00:00")
					.WithCreatedAt(1, "10:15:00")
					.WithCreatedAt(2, "10:45:00")
					.WithCreatedAt(3, "10:30:00") // out of order
					.WithCreatedAt(4, "11:00:00")
					.GetRecords();

				var dialog = Substitute.For<IUserDialog>();
				dialog
					.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
					.Returns("0");

				var analyzer = new TemporalAnomalyAnalyzer();
				analyzer.Analyze(records, string.Empty, dialog, canUpdateMetadata: true);

				Assert.AreEqual("TemporalAnomaly: -00:15:00", records[3].Metadata.Comment);
			}
		}
	}
}
