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
			IRecord record = HeldAsBytes(Encoding.UTF8, "First\nSecond\rThird\r\nFourth", isMultiLine: true);

			record.Content.Should().Be("First\r\nSecond\r\nThird\r\nFourth");
		}

		[TestMethod]
		public void GivenRecordsHeldAsBytes_WhenReadThroughOneBuffer_ThenEachIsItsContent()
		{
			// UTF-16, and a longer record after a shorter one, so the buffer grows.
			var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
			IRecord[] records =
			[
				HeldAsBytes(encoding, "Short", isMultiLine: false),
				HeldAsBytes(encoding, "A longer record\nwith a second line, and \u00e9", isMultiLine: true),
				HeldAsBytes(encoding, "Short again", isMultiLine: false),
			];
			var buffer = new ContentBuffer();

			foreach (IRecord record in records)
			{
				buffer.Read(record).ToString().Should().Be(record.Content);
			}
		}

		[TestMethod]
		public void GivenTheBytesAChunkWasMadeFrom_WhenChangedAfterwards_ThenTheRecordsContentDoesNotChange()
		{
			var bytes = Encoding.UTF8.GetBytes("Original");
			var record = new Record(1, DateTime.Now, SeverityType.Information, new ContentChunk(0, bytes, Encoding.UTF8), 0, bytes.Length, isMultiLine: false, wasGeneratedByUi: false);

			bytes[0] = (byte)'X';

			record.Content.Should().Be("Original");
		}

		private static IRecord HeldAsBytes(Encoding encoding, string text, bool isMultiLine)
		{
			// Bytes before the content, as in a chunk shared with other records.
			var bytes = encoding.GetBytes("Earlier\n" + text);
			var offset = encoding.GetByteCount("Earlier\n");
			var chunk = new ContentChunk(0, bytes, encoding);

			return new Record(1, DateTime.Now, SeverityType.Information, chunk, offset, bytes.Length - offset, isMultiLine, wasGeneratedByUi: false);
		}
	}
}
