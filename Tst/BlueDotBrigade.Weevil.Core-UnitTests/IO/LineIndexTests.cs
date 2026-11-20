namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class LineIndexTests
	{
		private static readonly int[] CheckpointIntervals = { 1, 2, 3, LineIndex.DefaultCheckpointInterval };

		// Small buffers put line breaks, and code units, across the boundary between reads.
		private static readonly int[] BufferSizes = { 4, 5, 7, LineIndex.DefaultBufferSize };

		internal static MemoryStream ToStream(string content, Encoding encoding)
		{
			return new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray());
		}

		private static List<string> ReadAllLines(Stream source)
		{
			var lines = new List<string>();

			source.Position = 0;
			using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

			string line;
			while ((line = reader.ReadLine()) != null)
			{
				lines.Add(line);
			}

			return lines;
		}

		private static string ReadLineAt(Stream source, long byteOffset, Encoding encoding)
		{
			source.Position = byteOffset;
			using var reader = new StreamReader(source, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

			return reader.ReadLine();
		}

		internal static void ShouldMatchReadLine(LineIndex index, Stream source, Encoding encodingWithoutPreamble)
		{
			List<string> expectedLines = ReadAllLines(source);

			index.LineCount.Should().Be(expectedLines.Count);

			for (var lineNumber = 1; lineNumber <= expectedLines.Count; lineNumber++)
			{
				var byteOffset = index.GetByteOffset(source, lineNumber, bufferSize: 5);

				ReadLineAt(source, byteOffset, encodingWithoutPreamble).Should().Be(expectedLines[lineNumber - 1], $"line {lineNumber} begins at byte {byteOffset}");
			}
		}

		[TestMethod]
		[WorkItem(1252)]
		public void GivenAsManyLinesAsWeevilCanNumber_WhenAnotherLineIsFound_ThenTheMessageSaysTheLogIsTooLarge()
		{
			// A log of more than 2,147,483,647 lines failed to open with a raw OverflowException.
			LineIndex.CountLine(LineIndex.MaximumLineCount - 1).Should().Be(LineIndex.MaximumLineCount);

			Action countingOneMore = () => LineIndex.CountLine(LineIndex.MaximumLineCount);

			countingOneMore.Should().Throw<NotSupportedException>()
				.WithMessage("Weevil can open logs of up to 2,147,483,590 lines. This log has more.");
		}

		[TestMethod]
		[DataRow("", DisplayName = "Empty")]
		[DataRow("x", DisplayName = "One line without a line break")]
		[DataRow("a\nb\nc", DisplayName = "LF")]
		[DataRow("a\r\nb\r\nc\r\n", DisplayName = "CRLF, ending with a line break")]
		[DataRow("a\rb\rc", DisplayName = "CR")]
		[DataRow("a\r\r\nb\n\rc\n\n", DisplayName = "Mixed")]
		[DataRow("\n\n\n", DisplayName = "Empty lines")]
		[DataRow("\r\n", DisplayName = "Only a line break")]
		[DataRow("héllo\nwörld\r\n日本語\n\U0001F600", DisplayName = "Multi-byte characters")]
		public void GivenLineBreaks_WhenIndexing_ThenEachOffsetMatchesReadLine(string content)
		{
			foreach (var checkpointInterval in CheckpointIntervals)
			{
				foreach (var bufferSize in BufferSizes)
				{
					using MemoryStream source = ToStream(content, new UTF8Encoding(false));

					var index = LineIndex.Build(source, checkpointInterval, bufferSize);

					ShouldMatchReadLine(index, source, new UTF8Encoding(false));
				}
			}
		}

		[TestMethod]
		public void GivenByteOrderMarks_WhenIndexing_ThenEachOffsetMatchesReadLine()
		{
			const string content = "First line\r\nSecond line\nThird é\rFourth \U0001F600\r\n";

			var encodings = new (Encoding WithPreamble, Encoding WithoutPreamble)[]
			{
				(new UTF8Encoding(true), new UTF8Encoding(false)),
				(new UnicodeEncoding(bigEndian: false, byteOrderMark: true), new UnicodeEncoding(false, false)),
				(new UnicodeEncoding(bigEndian: true, byteOrderMark: true), new UnicodeEncoding(true, false)),
				(new UTF32Encoding(bigEndian: false, byteOrderMark: true), new UTF32Encoding(false, false)),
				(new UTF32Encoding(bigEndian: true, byteOrderMark: true), new UTF32Encoding(true, false)),
			};

			foreach ((Encoding withPreamble, Encoding withoutPreamble) in encodings)
			{
				foreach (var bufferSize in BufferSizes)
				{
					using MemoryStream source = ToStream(content, withPreamble);

					var index = LineIndex.Build(source, checkpointInterval: 2, bufferSize);

					ShouldMatchReadLine(index, source, withoutPreamble);
				}
			}
		}

		[TestMethod]
		public void GivenOnlyByteOrderMark_WhenIndexing_ThenHasNoLines()
		{
			using MemoryStream source = ToStream(string.Empty, new UTF8Encoding(true));

			LineIndex.Build(source).LineCount.Should().Be(0);
		}

		[TestMethod]
		public void GivenStreamPosition_WhenIndexing_ThenPositionIsRestored()
		{
			using MemoryStream source = ToStream("a\nb\nc\n", new UTF8Encoding(false));
			source.Position = 3;

			var index = LineIndex.Build(source, checkpointInterval: 2);
			index.GetByteOffset(source, 2);

			source.Position.Should().Be(3);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(4)]
		public void GivenLineNumberOutsideLog_WhenGettingOffset_ThenThrows(int lineNumber)
		{
			using MemoryStream source = ToStream("a\nb\nc", new UTF8Encoding(false));
			var index = LineIndex.Build(source);

			Action act = () => index.GetByteOffset(source, lineNumber);

			act.Should().Throw<ArgumentOutOfRangeException>();
		}

		[TestMethod]
		[DataRow("a\r", "\nb\n", DisplayName = "CRLF split by the growth")]
		[DataRow("a\r", "b\n", DisplayName = "CR before the growth")]
		[DataRow("abc", "def\nghi", DisplayName = "Line continued by the growth")]
		[DataRow("a\n", "b\nc", DisplayName = "Growth after a line break")]
		public void GivenLogHasGrown_WhenUpdating_ThenMatchesIndexOfWholeLog(string original, string appended)
		{
			using var source = new MemoryStream();
			source.Write(Encoding.UTF8.GetBytes(original));

			var index = LineIndex.Build(source, checkpointInterval: 2, bufferSize: 4);

			source.Write(Encoding.UTF8.GetBytes(appended));
			index.Update(source, bufferSize: 4);

			ShouldMatchReadLine(index, source, new UTF8Encoding(false));
			index.IndexedLength.Should().Be(source.Length);
		}

		[TestMethod]
		[DataRow("", DisplayName = "Empty log")]
		[DataRow("\u00ef\u00bb", DisplayName = "Part of a byte order mark")]
		public void GivenByteOrderMarkCompletedLater_WhenUpdating_ThenMatchesIndexOfWholeLog(string originalLatin1)
		{
			var completeLog = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("a\nb\n")).ToArray();
			var originalLength = Encoding.Latin1.GetByteCount(originalLatin1);
			using var source = new MemoryStream();
			source.Write(completeLog, 0, originalLength);

			var index = LineIndex.Build(source, checkpointInterval: 1);

			source.Write(completeLog, originalLength, completeLog.Length - originalLength);
			index.Update(source);

			ShouldMatchReadLine(index, source, new UTF8Encoding(false));
			index.GetByteOffset(source, 1).Should().Be(3);
		}

		[TestMethod]
		public void GivenEmptyLog_WhenUtf16ContentIsAppended_ThenUpdateMatchesReadLine()
		{
			using var source = new MemoryStream();
			var index = LineIndex.Build(source);

			var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
			source.Write(encoding.GetPreamble());
			source.Write(encoding.GetBytes("First\r\nSecond\n"));
			index.Update(source);

			ShouldMatchReadLine(index, source, new UnicodeEncoding(false, false));
		}

		[TestMethod]
		[DataRow(0, DisplayName = "First byte changed")]
		[DataRow(-1, DisplayName = "Last byte changed")]
		public void GivenLogContentChanged_WhenMatched_ThenReturnsFalse(int changedOffset)
		{
			var bytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("A line of text\n", 1000)));
			LineIndex index = LineIndex.Build(new MemoryStream(bytes));

			bytes[changedOffset >= 0 ? changedOffset : bytes.Length + changedOffset] = (byte)'X';

			index.Matches(new MemoryStream(bytes)).Should().BeFalse();
		}

		[TestMethod]
		public void GivenLogIsShorterThanIndexed_WhenMatched_ThenReturnsFalse()
		{
			var bytes = Encoding.UTF8.GetBytes("a\nb\nc\n");
			LineIndex index = LineIndex.Build(new MemoryStream(bytes));

			index.Matches(new MemoryStream(bytes, 0, 4)).Should().BeFalse();
		}

		[TestMethod]
		public void GivenByteOrderMarkAdded_WhenMatched_ThenReturnsFalse()
		{
			using MemoryStream withoutMark = ToStream("a\nb\n", new UTF8Encoding(false));
			LineIndex index = LineIndex.Build(withoutMark);

			using MemoryStream withMark = ToStream("a\nb\n", new UTF8Encoding(true));

			index.Matches(withMark).Should().BeFalse();
		}

		[TestMethod]
		public void GivenCopiedLog_WhenMatched_ThenReturnsTrue()
		{
			var bytes = Encoding.UTF8.GetBytes("a\nb\nc\n");
			LineIndex index = LineIndex.Build(new MemoryStream(bytes));

			index.Matches(new MemoryStream(bytes.ToArray())).Should().BeTrue();
		}

		[TestMethod]
		public void GivenEveryByteOrderMarkAndLineBreak_WhenIndexedAndRead_ThenLogLineReaderFindsTheSameLineOffsets()
		{
			// The two scanners must agree, or a start-line open and the sidecar's byte offsets point at the wrong line (#1256).
			Encoding[] encodings =
			[
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
				new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
				new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
				new UTF32Encoding(bigEndian: false, byteOrderMark: true),
				new UTF32Encoding(bigEndian: true, byteOrderMark: true),
			];
			string[] texts =
			[
				"", "x", "a\nb\nc", "a\r\nb\r\nc\r\n", "a\rb\rc", "a\r\r\nb\n\rc\n\n", "\n\n\n", "\r\n", "a\r", "x\r\ry\n",
				"h\u00e9llo\nw\u00f6rld\r\n\u65e5\u672c\u8a9e\n\U0001F600",
			];

			foreach (Encoding encoding in encodings)
			{
				foreach (var text in texts)
				{
					foreach (var bufferSize in BufferSizes)
					{
						using MemoryStream source = ToStream(text, encoding);
						LineIndex index = LineIndex.Build(source, checkpointInterval: 2, bufferSize);

						var reader = new LogLineReader(source);
						var offsets = new List<long>();
						while (reader.TryReadLine(out var offset, out _, out _))
						{
							offsets.Add(offset);
						}

						var because = $"{encoding.WebName} with a byte order mark: {encoding.GetPreamble().Length > 0}, {text.Length} characters, a buffer of {bufferSize} bytes";
						index.LineCount.Should().Be(offsets.Count, because);
						Enumerable.Range(1, index.LineCount).Select(lineNumber => index.GetByteOffset(source, lineNumber, bufferSize: 5)).Should().Equal(offsets, because);
					}
				}
			}
		}
	}
}
