namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Linq;
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
			// Two records in a row from a chunk read it whole. The second chunk is longer, so the buffer's array grows when it moves to it.
			(LogFileSource source, _) = CreateLog("First\nSecond\nThe third line\nThe fourth line, the longest\n");
			var first = new ContentChunk(source, 0, 13, Encoding.UTF8);
			var second = new ContentChunk(source, 13, 44, Encoding.UTF8);
			var buffer = new ContentBuffer();

			buffer.Read(CreateRecord(1, first, 0, 5)).ToString().Should().Be("First");
			buffer.Read(CreateRecord(2, first, 6, 6)).ToString().Should().Be("Second");
			buffer.Read(CreateRecord(3, second, 0, 14)).ToString().Should().Be("The third line");
			buffer.Read(CreateRecord(4, second, 15, 28)).ToString().Should().Be("The fourth line, the longest");
			buffer.Read(CreateRecord(2, first, 6, 6)).ToString().Should().Be("Second");
			buffer.Read(CreateRecord(3, second, 0, 14)).ToString().Should().Be("The third line");
		}

		[TestMethod]
		public void GivenALogChangedFarFromARecord_WhenItsChunkIsReadOneRecordThenTwo_ThenOnlyTheScanFindsTheChange()
		{
			// A sparse list, such as a few filter results, reads only each record's blocks; a scan reads the whole chunk (#1027).
			var lines = string.Concat(Enumerable.Range(1, 30_000).Select(i => $"Line {i:D5}\n"));
			(LogFileSource source, var logPath) = CreateLog(lines);
			var chunk = new ContentChunk(source, 0, lines.Length, Encoding.UTF8);
			var buffer = new ContentBuffer();
			using (FileStream log = File.OpenWrite(logPath))
			{
				// Past the first 64 KB block, which holds the records read.
				log.Position = lines.Length - 1;
				log.WriteByte((byte)'x');
			}

			buffer.Read(CreateRecord(1, chunk, 0, 10)).ToString().Should().Be("Line 00001");
			Action readASecondRecord = () => buffer.Read(CreateRecord(2, chunk, 11, 10));

			readASecondRecord.Should().Throw<LogFileChangedException>("two records in a row read the whole chunk");
		}

		[TestMethod]
		public void GivenAChunkAlreadyReadWhole_WhenAPassComesBackToIt_ThenOnlyEachRecordsBlocksAreRead()
		{
			// Records sorted by time can go back and forth between chunks: each chunk is read whole once at most (#1027).
			var lines = string.Concat(Enumerable.Range(1, 30_000).Select(i => $"Line {i:D5}\n"));
			(LogFileSource source, var logPath) = CreateLog(lines);
			const int SecondChunkStart = 15_000 * 11;
			var first = new ContentChunk(source, 0, SecondChunkStart, Encoding.UTF8);
			var second = new ContentChunk(source, SecondChunkStart, lines.Length - SecondChunkStart, Encoding.UTF8);
			var buffer = new ContentBuffer();
			buffer.Read(CreateRecord(1, first, 0, 10));
			buffer.Read(CreateRecord(2, first, 11, 10));
			buffer.Read(CreateRecord(15_001, second, 0, 10));
			buffer.Read(CreateRecord(15_002, second, 11, 10));
			using (FileStream log = File.OpenWrite(logPath))
			{
				// In the first chunk, past the 64 KB block that holds the records read again.
				log.Position = SecondChunkStart - 1;
				log.WriteByte((byte)'x');
			}

			buffer.Read(CreateRecord(1, first, 0, 10)).ToString().Should().Be("Line 00001");
			buffer.Read(CreateRecord(2, first, 11, 10)).ToString().Should().Be("Line 00002", "the first chunk is not read whole again");
		}

		[TestMethod]
		public void GivenALogThatChanged_WhenAChunkIsReadAfterAnotherFailedToBe_ThenTheChangeIsReportedRatherThanOtherText()
		{
			// The longer chunk that fails to be read replaces the buffer's array, which no longer holds the first chunk.
			// The first chunk is read twice in a row, so the buffer holds all of it.
			(LogFileSource source, var logPath) = CreateLog("First\nSecond, a longer line\n");
			var shorter = new ContentChunk(source, 0, 6, Encoding.UTF8);
			var longer = new ContentChunk(source, 6, 22, Encoding.UTF8);
			var buffer = new ContentBuffer();
			buffer.Read(CreateRecord(1, shorter, 0, 5)).ToString().Should().Be("First");
			buffer.Read(CreateRecord(1, shorter, 0, 5)).ToString().Should().Be("First");
			buffer.Read(CreateRecord(2, longer, 0, 21)).ToString().Should().Be("Second, a longer line");
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

			var builder = new LogFileSourceBuilder(logPath, 0, bytes.Length);
			builder.Append(bytes);

			return (builder.Build(), logPath);
		}

		private static Record CreateRecord(int lineNumber, ContentChunk chunk, int offset, int length)
		{
			return new Record(lineNumber, DateTime.Now, SeverityType.Information, chunk, offset, length, isMultiLine: false, wasGeneratedByUi: false);
		}
	}
}
