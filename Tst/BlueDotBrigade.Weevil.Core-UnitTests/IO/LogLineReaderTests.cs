namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

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

		[TestMethod]
		public void GivenEveryByteOrderMarkAndLineBreak_WhenRead_ThenTheLinesAreThoseOfStreamReader()
		{
			Encoding[] encodings =
			[
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
				new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
				new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
				new UTF32Encoding(bigEndian: false, byteOrderMark: true),
				new UTF32Encoding(bigEndian: true, byteOrderMark: true),
			];
			string[] texts = ["a\nb\rc\r\nd", "a\r", "\r\n\r\n", "x\r\ry\n", "\u00e9\r\u65e5\n\U0001F600"];

			foreach (Encoding encoding in encodings)
			{
				foreach (var text in texts)
				{
					var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();

					// Run with a time limit, so a reader that never ends fails the test instead of stopping the test run.
					var reading = Task.Run(() => ReadAll(new LogLineReader(new MemoryStream(bytes))));
					reading.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue($"reading {encoding.WebName} should end");

					List<(string Text, long Offset, int Length)> lines = reading.Result;
					lines.Select(line => line.Text).Should().Equal(ReadWithStreamReader(bytes), $"{encoding.WebName}, {text.Length} characters");

					var decoder = new LogLineReader(new MemoryStream(bytes)).Encoding;
					foreach ((string lineText, long offset, int length) in lines)
					{
						decoder.GetString(bytes, (int)offset, length).Should().Be(lineText, "each line's offset and length point at its bytes");
					}
				}
			}
		}

		private static List<string> ReadWithStreamReader(byte[] bytes)
		{
			var lines = new List<string>();

			using var reader = new StreamReader(new MemoryStream(bytes));
			for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
			{
				lines.Add(line);
			}

			return lines;
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
