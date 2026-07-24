namespace BlueDotBrigade.Weevil.Analysis
{
	[TestClass]
	public class AnalysisManagerTests
	{
		[TestMethod]
		[DataRow("NoRecords.log", 0)]
		[DataRow("CompleteRecord.log", 1)]
		[DataRow("ThreeRecords.log", 3)]
		public void GivenRecordCollection_WhenRemoveAllFlagsCalled_ThenEveryRecordIsUnflagged(string fileName, int expectedCount)
		{
			// Regression: Issue #932
			IEngine engine = OpenEngine(fileName);

			foreach (var record in engine.Records)
			{
				record.Metadata.IsFlagged = true;
			}

			engine.Analyzer.RemoveAllFlags();

			engine.Records.Should().HaveCount(expectedCount);
			engine.Records.Should().NotContain(record => record.Metadata.IsFlagged);
		}

		[TestMethod]
		[DataRow("NoRecords.log", 0)]
		[DataRow("CompleteRecord.log", 1)]
		[DataRow("ThreeRecords.log", 3)]
		public void GivenRecordCollection_WhenUnpinAllCalled_ThenEveryRecordIsUnpinned(string fileName, int expectedCount)
		{
			// Regression: Issue #932
			IEngine engine = OpenEngine(fileName);

			foreach (var record in engine.Records)
			{
				record.Metadata.IsPinned = true;
			}

			engine.Analyzer.UnpinAll();

			engine.Records.Should().HaveCount(expectedCount);
			engine.Records.Should().NotContain(record => record.Metadata.IsPinned);
		}

		private static IEngine OpenEngine(string fileName)
		{
			return Engine
				.UsingPath(new Daten().AsFilePath(fileName))
				.Open();
		}
	}
}
