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
		public void GivenTwoRecordsForTheSameLine_WhenCompared_ThenTheyAreEqual()
		{
			// One holds a string and the other the log's bytes: only the line number counts (#1135).
			IRecord first = R.WithContent(7, "First");
			IRecord second = R.WithContentAsBytes(7, "Second", Encoding.UTF8, isMultiLine: false);

			first.Should().Be(second);
			first.GetHashCode().Should().Be(second.GetHashCode());
		}

		[TestMethod]
		public void GivenRecordsForDifferentLines_WhenCompared_ThenTheyAreNotEqual()
		{
			R.WithContent(7, "Same").Should().NotBe(R.WithContent(8, "Same"));
		}

		[TestMethod]
		public void GivenARecordOnLineMinusOne_WhenCheckedForTheDummy_ThenItIsTheDummy()
		{
			// Line -1 is the dummy's, and a log's line numbers start at 1 (#1135).
			Record.IsDummyOrNull(R.WithContent(-1, "Any")).Should().BeTrue();
			Record.IsGenuine(R.WithContent(1, "Any")).Should().BeTrue();
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
