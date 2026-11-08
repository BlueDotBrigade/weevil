namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;
	using Directory = System.IO.Directory;
	using File = System.IO.File;

	[TestClass]
	public class ContentBufferTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = Directory.CreateTempSubdirectory("Weevil.ContentBufferTests.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenRecordsHeldAsBytes_WhenReadThroughOneBuffer_ThenEachIsItsContent()
		{
			// UTF-16, and a longer record after a shorter one, so the buffer grows.
			var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
			IRecord[] records =
			[
				R.WithContentAsBytes(1, "Short", encoding, isMultiLine: false),
				R.WithContentAsBytes(2, "A longer record\nwith a second line, and é", encoding, isMultiLine: true),
				R.WithContentAsBytes(3, "Short again", encoding, isMultiLine: false),
			];
			var buffer = new ContentBuffer();

			foreach (IRecord record in records)
			{
				buffer.Read(record).ToString().Should().Be(record.Content);
			}
		}

		[TestMethod]
		public void GivenRecordsInTwoChunksOnFile_WhenReadThroughOneBufferAndBack_ThenEachIsItsContent()
		{
			// The second chunk is longer, so the buffer's array grows when it moves to it.
			(LogFileSource source, _) = CreateLog("First\nSecond\nThe third line\nThe fourth line, the longest\n");
			var first = new ContentChunk(source, 0, 13, Encoding.UTF8);
			var second = new ContentChunk(source, 13, 44, Encoding.UTF8);
			var buffer = new ContentBuffer();

			buffer.Read(CreateRecord(1, first, 0, 5)).ToString().Should().Be("First");
			buffer.Read(CreateRecord(3, second, 0, 14)).ToString().Should().Be("The third line");
			buffer.Read(CreateRecord(2, first, 6, 6)).ToString().Should().Be("Second");
			buffer.Read(CreateRecord(4, second, 15, 28)).ToString().Should().Be("The fourth line, the longest");
		}

		[TestMethod]
		public void GivenALogThatChanged_WhenAChunkIsReadAfterAnotherFailedToBe_ThenTheChangeIsReportedRatherThanOtherText()
		{
			// The longer chunk that fails to be read replaces the buffer's array, which no longer holds the first chunk.
			(LogFileSource source, var logPath) = CreateLog("First\nSecond, a longer line\n");
			var shorter = new ContentChunk(source, 0, 6, Encoding.UTF8);
			var longer = new ContentChunk(source, 6, 22, Encoding.UTF8);
			var buffer = new ContentBuffer();
			buffer.Read(CreateRecord(1, shorter, 0, 5)).ToString().Should().Be("First");
			File.WriteAllText(logPath, "Other\nLines of the same size");

			Action readTheLongerChunk = () => buffer.Read(CreateRecord(2, longer, 0, 21));
			Action readTheShorterChunkAgain = () => buffer.Read(CreateRecord(1, shorter, 0, 5));

			readTheLongerChunk.Should().Throw<LogFileChangedException>();
			readTheShorterChunkAgain.Should().Throw<LogFileChangedException>();
		}

		private (LogFileSource Source, string LogPath) CreateLog(string text)
		{
			var logPath = Path.Combine(_folder, "Log.txt");
			var bytes = Encoding.UTF8.GetBytes(text);
			File.WriteAllBytes(logPath, bytes);

			var builder = new LogFileSourceBuilder(logPath, 0);
			builder.Append(bytes);

			return (builder.Build(), logPath);
		}

		private static Record CreateRecord(int lineNumber, ContentChunk chunk, int offset, int length)
		{
			return new Record(lineNumber, DateTime.Now, SeverityType.Information, chunk, offset, length, isMultiLine: false, wasGeneratedByUi: false);
		}
	}
}
