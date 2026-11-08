namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using NSubstitute;

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
		[DataRow(StartOffset, LogLength, StartOffset, 10, DisplayName = "The first bytes parsed")]
		[DataRow(StartOffset, LogLength, 1000, 500, DisplayName = "Within a block")]
		[DataRow(StartOffset, LogLength, LogFileSource.BlockSize - 100, 200, DisplayName = "Across a block boundary")]
		[DataRow(StartOffset, LogLength, 100, (2 * LogFileSource.BlockSize) + 50, DisplayName = "Across several blocks")]
		[DataRow(StartOffset, LogLength, LogLength - 300, 300, DisplayName = "The last bytes parsed")]
		[DataRow(StartOffset, 2 * LogFileSource.BlockSize, (2 * LogFileSource.BlockSize) - 100, 100, DisplayName = "The last bytes parsed, ending on a block boundary")]
		[DataRow((2 * LogFileSource.BlockSize) + 100, LogLength, (2 * LogFileSource.BlockSize) + 150, LogFileSource.BlockSize, DisplayName = "Parsed from past the first blocks")]
		public void GivenALogParsed_WhenBytesAreReadBack_ThenTheyAreTheBytesParsed(int startOffset, int endOffset, int fileOffset, int length)
		{
			LogFileSource source = Parse(startOffset, endOffset);
			var destination = new byte[length];

			source.Read(fileOffset, destination);

			destination.Should().Equal(_bytes.AsSpan(fileOffset, length).ToArray());
			source.HasGrown.Should().BeFalse();
		}

		[TestMethod]
		public void GivenALogStillBeingParsed_WhenRead_ThenTheReadIsRefused()
		{
			var builder = new LogFileSourceBuilder(_logPath, 0, LogLength);
			builder.Append(_bytes.AsSpan(0, 100));

			Action read = () => builder.Source.Read(0, new byte[10]);

			read.Should().Throw<InvalidOperationException>("the last block's checksum is not known yet");
		}

		[TestMethod]
		public void GivenAChunkMadeWhileTheLogIsParsed_WhenReadOnceTheSourceIsBuilt_ThenItHoldsTheBytesParsed()
		{
			// The parser makes each chunk on file as soon as its bytes have been read, before the rest of the log.
			var builder = new LogFileSourceBuilder(_logPath, 0, LogLength);
			builder.Append(_bytes.AsSpan(0, 1000));
			var chunk = new ContentChunk(builder.Source, 100, 900, Encoding.UTF8);
			builder.Append(_bytes.AsSpan(1000));
			builder.Build();

			new ContentBuffer().Read(chunk, 0, 900, isMultiLine: false).ToString()
				.Should().Be(Encoding.UTF8.GetString(_bytes, 100, 900));
		}

		[TestMethod]
		public void GivenATruncatedLog_WhenRead_ThenItIsReportedAsChangedWithBothLengths()
		{
			LogFileSource source = Parse(StartOffset, LogLength);
			System.IO.File.WriteAllBytes(_logPath, _bytes.AsSpan(0, LogLength - 10).ToArray());

			Action read = () => source.Read(StartOffset, new byte[10]);

			LogFileChangedException changed = read.Should().Throw<LogFileChangedException>().Which;
			changed.ExpectedLength.Should().Be(LogLength);
			changed.ActualLength.Should().Be(LogLength - 10);
		}

		[TestMethod]
		[DataRow(LogLength, DisplayName = "Of the same length")]
		[DataRow(LogLength + 1000, DisplayName = "Longer")]
		public void GivenALogReplacedByAnother_WhenRead_ThenItIsReportedAsChangedAndNotAsGrown(int replacementLength)
		{
			LogFileSource source = Parse(StartOffset, LogLength);
			System.IO.File.WriteAllBytes(_logPath, Enumerable.Repeat((byte)'x', replacementLength).ToArray());

			Action read = () => source.Read(StartOffset, new byte[10]);

			read.Should().Throw<LogFileChangedException>().Which.ChangedOffset.Should().Be(StartOffset);
			source.HasGrown.Should().BeFalse();
		}

		[TestMethod]
		public void GivenOneByteChangedInTheMiddleOfTheLog_WhenItsBlockIsRead_ThenTheBlockIsReportedAsChanged()
		{
			LogFileSource source = Parse(StartOffset, LogLength);
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
		public void GivenALogThatGrew_WhenReadTwice_ThenTheBytesParsedAreReadAndAWarningIsLoggedOnce()
		{
			LogFileSource source = Parse(StartOffset, LogLength);
			System.IO.File.AppendAllText(_logPath, "A line written after the log was opened\r\n");
			var destination = new byte[300];
			ILogWriter original = Log.Default;
			ILogWriter writer = Substitute.For<ILogWriter>();
			Log.Register(writer);

			try
			{
				source.Read(LogLength - 300, destination);
				source.Read(StartOffset, new byte[10]);
			}
			finally
			{
				Log.Register(original);
			}

			destination.Should().Equal(_bytes.AsSpan(LogLength - 300).ToArray());
			source.HasGrown.Should().BeTrue();
			writer.Received(1).Write(LogSeverityType.Warning, Arg.Is<string>(message => message.Contains("has grown")));
		}

		[TestMethod]
		public void GivenALogParsedOnlyInPart_WhenRead_ThenItIsNotReportedAsGrown()
		{
			// As when a log is opened with a limit on its records: the rest of the log was there when it was opened.
			LogFileSource source = Parse(StartOffset, LogLength - 1000);

			source.Read(StartOffset, new byte[10]);

			source.HasGrown.Should().BeFalse();
		}

		[TestMethod]
		public void GivenAnApplicationWritingTheLog_WhenRead_ThenNeitherBlocksTheOther()
		{
			LogFileSource source = Parse(StartOffset, LogLength);

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
			LogFileSource source = Parse(StartOffset, LogLength);
			System.IO.File.Delete(_logPath);
			var stopwatch = Stopwatch.StartNew();

			Action read = () => source.Read(StartOffset, new byte[10]);

			read.Should().Throw<FileNotFoundException>();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1), "a read never waits for the log, as FileHelper.Open does for 2 seconds");
		}

		/// <summary>
		/// Appends the log's bytes from <paramref name="startOffset"/> to <paramref name="endOffset"/>,
		/// in pieces that do not line up with the blocks, as a parser reads them.
		/// </summary>
		private LogFileSource Parse(int startOffset, int endOffset)
		{
			const int PieceSize = 7919;
			var builder = new LogFileSourceBuilder(_logPath, startOffset, LogLength);

			for (var offset = startOffset; offset < endOffset; offset += PieceSize)
			{
				builder.Append(_bytes.AsSpan(offset, System.Math.Min(PieceSize, endOffset - offset)));
			}

			return builder.Build();
		}
	}
}
