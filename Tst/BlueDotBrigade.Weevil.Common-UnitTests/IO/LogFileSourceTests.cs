namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class LogFileSourceTests
	{
		// Parsing starts after a UTF-8 byte order mark, and ends within a block.
		private const int StartOffset = 3;
		private const int LogLength = (3 * LogFileSource.BlockSize) + 1234;

		private string _folder = string.Empty;
		private string _logPath = string.Empty;
		private byte[] _bytes = [];

		[TestInitialize]
		public void Setup()
		{
			_folder = System.IO.Directory.CreateTempSubdirectory("Weevil.LogFileSourceTests.").FullName;
			_logPath = Path.Combine(_folder, "Log.txt");

			var text = string.Concat(Enumerable.Range(1, 20_000).Select(i => $"Line {i}\r\n"));
			_bytes = Encoding.UTF8.GetBytes(text).AsSpan(0, LogLength).ToArray();
			System.IO.File.WriteAllBytes(_logPath, _bytes);
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[DataRow(StartOffset, 10, DisplayName = "The first bytes parsed")]
		[DataRow(1000, 500, DisplayName = "Within a block")]
		[DataRow(LogFileSource.BlockSize - 100, 200, DisplayName = "Across a block boundary")]
		[DataRow(100, (2 * LogFileSource.BlockSize) + 50, DisplayName = "Across several blocks")]
		[DataRow(LogLength - 300, 300, DisplayName = "The last bytes parsed")]
		public void GivenALogParsed_WhenBytesAreReadBack_ThenTheyAreTheBytesParsed(int fileOffset, int length)
		{
			LogFileSource source = Parse();
			var destination = new byte[length];

			source.Read(fileOffset, destination);

			destination.Should().Equal(_bytes.AsSpan(fileOffset, length).ToArray());
			source.HasGrown.Should().BeFalse();
		}

		[TestMethod]
		public void GivenATruncatedLog_WhenRead_ThenItIsReportedAsChangedWithBothLengths()
		{
			LogFileSource source = Parse();
			System.IO.File.WriteAllBytes(_logPath, _bytes.AsSpan(0, LogLength - 10).ToArray());

			Action read = () => source.Read(StartOffset, new byte[10]);

			LogFileChangedException changed = read.Should().Throw<LogFileChangedException>().Which;
			changed.ExpectedLength.Should().Be(LogLength);
			changed.ActualLength.Should().Be(LogLength - 10);
		}

		[TestMethod]
		public void GivenALogReplacedByOneOfTheSameLength_WhenRead_ThenItIsReportedAsChanged()
		{
			LogFileSource source = Parse();
			System.IO.File.WriteAllBytes(_logPath, Enumerable.Repeat((byte)'x', LogLength).ToArray());

			Action read = () => source.Read(StartOffset, new byte[10]);

			read.Should().Throw<LogFileChangedException>().Which.ChangedOffset.Should().Be(StartOffset);
		}

		[TestMethod]
		public void GivenOneByteChangedInTheMiddleOfTheLog_WhenItsBlockIsRead_ThenTheBlockIsReportedAsChanged()
		{
			LogFileSource source = Parse();
			const int ChangedByte = (2 * LogFileSource.BlockSize) + 777;
			using (FileStream log = System.IO.File.OpenWrite(_logPath))
			{
				log.Position = ChangedByte;
				log.WriteByte((byte)(_bytes[ChangedByte] ^ 1));
			}

			// A record near the changed byte, which it does not hold, is in the same block.
			Action read = () => source.Read(ChangedByte + 100, new byte[10]);

			read.Should().Throw<LogFileChangedException>().Which.ChangedOffset.Should().Be(2 * LogFileSource.BlockSize);
		}

		[TestMethod]
		public void GivenALogThatGrew_WhenRead_ThenTheBytesParsedAreReadAndTheLogIsReportedAsGrown()
		{
			LogFileSource source = Parse();
			System.IO.File.AppendAllText(_logPath, "A line written after the log was opened\r\n");
			var destination = new byte[300];

			source.Read(LogLength - 300, destination);

			destination.Should().Equal(_bytes.AsSpan(LogLength - 300).ToArray());
			source.HasGrown.Should().BeTrue();
		}

		[TestMethod]
		public void GivenAnApplicationWritingTheLog_WhenRead_ThenNeitherBlocksTheOther()
		{
			LogFileSource source = Parse();

			// A logger that lets others read, but not write.
			using (var writer = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
			{
				source.Read(StartOffset, new byte[10]);

				writer.Write(Encoding.UTF8.GetBytes("Written while Weevil reads\r\n"));
				writer.Flush();
			}

			// No handle is kept between reads, so a writer can even open the log without sharing it.
			using var exclusiveWriter = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.None);
			exclusiveWriter.Write(Encoding.UTF8.GetBytes("Written by a logger that shares nothing\r\n"));
		}

		[TestMethod]
		public void GivenALogThatIsGone_WhenRead_ThenTheReadFailsAtOnce()
		{
			LogFileSource source = Parse();
			System.IO.File.Delete(_logPath);
			var stopwatch = Stopwatch.StartNew();

			Action read = () => source.Read(StartOffset, new byte[10]);

			read.Should().Throw<FileNotFoundException>();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1), "a read never waits for the log, as FileHelper.Open does for 2 seconds");
		}

		/// <summary>
		/// Appends the log's bytes from the start offset, in pieces that do not line up with the blocks, as a parser reads them.
		/// </summary>
		private LogFileSource Parse()
		{
			var builder = new LogFileSourceBuilder(_logPath, StartOffset);

			for (var offset = StartOffset; offset < LogLength; offset += 7919)
			{
				builder.Append(_bytes.AsSpan(offset, System.Math.Min(7919, LogLength - offset)));
			}

			return builder.Build();
		}
	}
}
