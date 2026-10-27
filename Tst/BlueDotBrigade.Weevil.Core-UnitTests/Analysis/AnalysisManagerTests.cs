namespace BlueDotBrigade.Weevil.Analysis
{
	[TestClass]
	public class AnalysisManagerTests
	{
		[TestMethod]
		[DataRow("NoRecords.log", 0)]
		[DataRow("CompleteRecord.log", 1)]
		[DataRow("ThreeRecords.log", 3)]
		[WorkItem(932)]
		public void GivenRecordCollection_WhenRemoveAllFlagsCalled_ThenEveryRecordIsUnflagged(string fileName, int expectedCount)
		{
			// Remove All Flags and Unpin All skipped the last record.
			IEngine engine = OpenEngine(fileName);

			foreach (var record in engine.Records)
			{
				engine.RecordState.SetFlagged(record, true);
			}

			engine.Analyzer.RemoveAllFlags();

			engine.Records.Should().HaveCount(expectedCount);
			engine.Records.Should().NotContain(record => engine.RecordState.IsFlagged(record));
		}

		[TestMethod]
		[DataRow("NoRecords.log", 0)]
		[DataRow("CompleteRecord.log", 1)]
		[DataRow("ThreeRecords.log", 3)]
		[WorkItem(932)]
		public void GivenRecordCollection_WhenUnpinAllCalled_ThenEveryRecordIsUnpinned(string fileName, int expectedCount)
		{
			// Remove All Flags and Unpin All skipped the last record.
			IEngine engine = OpenEngine(fileName);

			foreach (var record in engine.Records)
			{
				engine.RecordState.SetPinned(record, true);
			}

			engine.Analyzer.UnpinAll();

			engine.Records.Should().HaveCount(expectedCount);
			engine.Records.Should().NotContain(record => engine.RecordState.IsPinned(record));
		}

		private static IEngine OpenEngine(string fileName)
		{
			return Engine
				.UsingPath(new Daten().AsFilePath(fileName))
				.Open();
		}
	}
}
