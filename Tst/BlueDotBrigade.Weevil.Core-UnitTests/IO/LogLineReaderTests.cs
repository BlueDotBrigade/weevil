namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class LogLineReaderTests
	{
		[TestMethod]
		public void GivenLinesEndingInLfCrAndCrLf_WhenRead_ThenEachLineAndWhereItStartsAreReturned()
		{
			var reader = new LogLineReader(new MemoryStream(Encoding.UTF8.GetBytes("a\nbc\r\rd\r\ne")));

			ReadAll(reader).Should().Equal(("a", 0, 1), ("bc", 2, 2), ("", 5, 0), ("d", 6, 1), ("e", 9, 1));
		}

		[TestMethod]
		public void GivenALineBreakAtTheEndOfTheFile_WhenRead_ThenNoEmptyLineFollows()
		{
			var reader = new LogLineReader(new MemoryStream(Encoding.UTF8.GetBytes("a\r\n")));

			ReadAll(reader).Should().Equal(("a", 0, 1));
		}

		[TestMethod]
		public void GivenAUtf16BigEndianLog_WhenRead_ThenLinesStartAfterTheByteOrderMark()
		{
			var bytes = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("ab\nc")).ToArray();
			var reader = new LogLineReader(new MemoryStream(bytes));

			ReadAll(reader).Should().Equal(("ab", 2, 4), ("c", 8, 2));
		}

		[TestMethod]
		public void GivenAStreamThatReturnsOneByteAtATime_WhenRead_ThenEachCrLfIsStillOneLineBreak()
		{
			// Each \r is the last byte read, so whether \n follows is only known after the next read.
			var reader = new LogLineReader(new OneByteAtATimeStream(Encoding.UTF8.GetBytes("x\r\ny\rz\n")));

			ReadAll(reader).Should().Equal(("x", 0, 1), ("y", 3, 1), ("z", 5, 1));
		}

		private static List<(string Text, long Offset, int Length)> ReadAll(LogLineReader reader)
		{
			var lines = new List<(string Text, long Offset, int Length)>();

			while (reader.TryReadLine(out var offset, out var length, out var text))
			{
				lines.Add((text, offset, length));
			}

			return lines;
		}

		private sealed class OneByteAtATimeStream(byte[] bytes) : MemoryStream(bytes)
		{
			public override int Read(byte[] buffer, int offset, int count)
			{
				return base.Read(buffer, offset, Math.Min(count, 1));
			}
		}
	}
}
