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

		private static MemoryStream ToStream(string content, Encoding encoding)
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

		private static void ShouldMatchReadLine(LineIndex index, Stream source, Encoding encodingWithoutPreamble)
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
		public void GivenSavedIndex_WhenReading_ThenOffsetsAreTheSame()
		{
			using MemoryStream source = ToStream("a\nbb\nccc\ndddd\neeeee", new UTF8Encoding(false));
			var original = LineIndex.Build(source, checkpointInterval: 2);
			using var saved = new MemoryStream();
			original.Write(saved);
			saved.Position = 0;

			var wasRead = LineIndex.TryRead(saved, source, out LineIndex index);

			wasRead.Should().BeTrue();
			index.LineCount.Should().Be(original.LineCount);
			index.IndexedLength.Should().Be(original.IndexedLength);
			ShouldMatchReadLine(index, source, new UTF8Encoding(false));
		}

		[TestMethod]
		public void GivenSavedIndexAndLogHasGrown_WhenReadingAndUpdating_ThenMatchesIndexOfWholeLog()
		{
			using var source = new MemoryStream();
			source.Write(Encoding.UTF8.GetBytes("a\nb\r"));
			using var saved = new MemoryStream();
			LineIndex.Build(source, checkpointInterval: 2).Write(saved);
			saved.Position = 0;
			source.Write(Encoding.UTF8.GetBytes("\nc\nd"));

			LineIndex.TryRead(saved, source, out LineIndex index).Should().BeTrue();
			index.Update(source);

			ShouldMatchReadLine(index, source, new UTF8Encoding(false));
		}

		[TestMethod]
		[DataRow(0, DisplayName = "First byte changed")]
		[DataRow(-1, DisplayName = "Last byte changed")]
		public void GivenLogContentChanged_WhenReadingSavedIndex_ThenReturnsFalse(int changedOffset)
		{
			var bytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("A line of text\n", 1000)));
			using var saved = new MemoryStream();
			LineIndex.Build(new MemoryStream(bytes)).Write(saved);
			saved.Position = 0;

			bytes[changedOffset >= 0 ? changedOffset : bytes.Length + changedOffset] = (byte)'X';

			LineIndex.TryRead(saved, new MemoryStream(bytes), out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenLogIsShorterThanIndexed_WhenReadingSavedIndex_ThenReturnsFalse()
		{
			var bytes = Encoding.UTF8.GetBytes("a\nb\nc\n");
			using var saved = new MemoryStream();
			LineIndex.Build(new MemoryStream(bytes)).Write(saved);
			saved.Position = 0;

			LineIndex.TryRead(saved, new MemoryStream(bytes, 0, 4), out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenCopiedLog_WhenReadingSavedIndex_ThenReturnsTrue()
		{
			var bytes = Encoding.UTF8.GetBytes("a\nb\nc\n");
			using var saved = new MemoryStream();
			LineIndex.Build(new MemoryStream(bytes)).Write(saved);
			saved.Position = 0;

			LineIndex.TryRead(saved, new MemoryStream(bytes.ToArray()), out _).Should().BeTrue();
		}

		[TestMethod]
		public void GivenTruncatedIndex_WhenReading_ThenReturnsFalse()
		{
			using MemoryStream source = ToStream("a\nb\nc\nd\n", new UTF8Encoding(false));
			using var saved = new MemoryStream();
			LineIndex.Build(source, checkpointInterval: 1).Write(saved);
			var truncated = new MemoryStream(saved.ToArray(), 0, (int)saved.Length - 1);

			LineIndex.TryRead(truncated, source, out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenAnyByteOfSavedIndexChanged_WhenReading_ThenReturnsFalse()
		{
			using MemoryStream source = ToStream("a\nb\nc\nd\n", new UTF8Encoding(false));
			using var saved = new MemoryStream();
			LineIndex.Build(source, checkpointInterval: 1).Write(saved);
			var original = saved.ToArray();

			for (var i = 0; i < original.Length; i++)
			{
				var damaged = original.ToArray();
				damaged[i] ^= 0x01;

				LineIndex.TryRead(new MemoryStream(damaged), source, out _).Should().BeFalse($"byte {i} was changed");
			}
		}

		[TestMethod]
		public void GivenFileThatIsNotAnIndex_WhenReading_ThenReturnsFalse()
		{
			using MemoryStream source = ToStream("a\nb\n", new UTF8Encoding(false));
			using MemoryStream notAnIndex = ToStream("This is not an index file.", new UTF8Encoding(false));

			LineIndex.TryRead(notAnIndex, source, out _).Should().BeFalse();
		}
	}
}
