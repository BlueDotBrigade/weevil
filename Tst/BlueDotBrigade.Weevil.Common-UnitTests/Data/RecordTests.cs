namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Text;

	[TestClass]
	public class RecordTests
	{
		[TestMethod]
		public void GivenEverySeverityAndFact_WhenRecordCreated_ThenEachIsKept()
		{
			foreach (SeverityType severity in Enum.GetValues<SeverityType>())
			{
				foreach (var isMultiLine in new[] { false, true })
				{
					foreach (var wasGeneratedByUi in new[] { false, true })
					{
						var record = new Record(1, DateTime.Now, severity, "content", isMultiLine, wasGeneratedByUi);

						record.Severity.Should().Be(severity);
						record.IsMultiLine.Should().Be(isMultiLine);
						record.WasGeneratedByUi.Should().Be(wasGeneratedByUi);
					}
				}
			}
		}

		[TestMethod]
		public void GivenRecordWithoutFacts_WhenCreated_ThenItIsSingleLineAndNotFromTheUiThread()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Warning, "content");

			record.Severity.Should().Be(SeverityType.Warning);
			record.IsMultiLine.Should().BeFalse();
			record.WasGeneratedByUi.Should().BeFalse();
		}

		[TestMethod]
		public void GivenUnknownSeverity_WhenRecordCreated_ThenThrows()
		{
			Action create = () => new Record(1, DateTime.Now, (SeverityType)8, "content");

			create.Should().Throw<ArgumentOutOfRangeException>();
		}

		[TestMethod]
		public void GivenAMultiLineRecordHeldAsBytes_WhenItsContentIsRead_ThenItsLinesAreJoinedWithCrLf()
		{
			// The file's own line breaks: \n, \r and \r\n.
			IRecord record = R.WithContentAsBytes(1, "First\nSecond\rThird\r\nFourth", Encoding.UTF8, isMultiLine: true);

			record.Content.Should().Be("First\r\nSecond\r\nThird\r\nFourth");
		}

		[TestMethod]
		public void GivenTheBytesAChunkWasMadeFrom_WhenChangedAfterwards_ThenTheRecordsContentDoesNotChange()
		{
			var bytes = Encoding.UTF8.GetBytes("Original");
			var record = new Record(1, DateTime.Now, SeverityType.Information, new ContentChunk(0, bytes, Encoding.UTF8), 0, bytes.Length, isMultiLine: false, wasGeneratedByUi: false);

			bytes[0] = (byte)'X';

			record.Content.Should().Be("Original");
		}
	}
}
