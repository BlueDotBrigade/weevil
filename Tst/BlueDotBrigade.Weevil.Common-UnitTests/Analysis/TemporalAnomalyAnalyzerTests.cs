namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

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

			var dialog = new Mock<IUserDialog>();
			dialog
				.Setup(x => x.ShowUserPrompt(
					It.IsAny<string>(), 
					It.IsAny<string>(),
					It.IsAny<string>()))
				.Returns("0");

			var analyzer = new TemporalAnomalyAnalyzer();
			analyzer.Analyze(records, string.Empty, dialog.Object, canUpdateMetadata: true);

			Assert.AreEqual("TemporalAnomaly: -00:15:00", records.RecordAtLineNumber(3).Metadata.Comment);
		}
	}
}
