namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class IndexFileTests
	{
		/// <summary>
		/// Reads a saved index as an open does: it must be intact, and the log must still have the content its line index scanned.
		/// </summary>
		private static bool TryRead(Stream saved, Stream log, out LineIndex lines, out IndexFile file)
		{
			return IndexFile.TryRead(saved, log.Length, out lines, out file) && lines.Matches(log);
		}

		[TestMethod]
		public void GivenSavedIndex_WhenReading_ThenOffsetsAreTheSame()
		{
			using MemoryStream source = LineIndexTests.ToStream("a\nbb\nccc\ndddd\neeeee", new UTF8Encoding(false));
			var original = LineIndex.Build(source, checkpointInterval: 2);
			using var saved = new MemoryStream();
			IndexFile.Write(saved, original);
			saved.Position = 0;

			var wasRead = TryRead(saved, source, out LineIndex index, out IndexFile file);

			wasRead.Should().BeTrue();
			index.LineCount.Should().Be(original.LineCount);
			index.IndexedLength.Should().Be(original.IndexedLength);
			file.Parse.Should().BeNull();
			file.HasTable.Should().BeFalse();
			LineIndexTests.ShouldMatchReadLine(index, source, new UTF8Encoding(false));
		}

		[TestMethod]
		public void GivenSavedIndexAndLogHasGrown_WhenReadingAndUpdating_ThenMatchesIndexOfWholeLog()
		{
			using var source = new MemoryStream();
			source.Write(Encoding.UTF8.GetBytes("a\nb\r"));
			using var saved = new MemoryStream();
			IndexFile.Write(saved, LineIndex.Build(source, checkpointInterval: 2));
			saved.Position = 0;
			source.Write(Encoding.UTF8.GetBytes("\nc\nd"));

			TryRead(saved, source, out LineIndex index, out _).Should().BeTrue();
			index.Update(source);

			LineIndexTests.ShouldMatchReadLine(index, source, new UTF8Encoding(false));
		}

		[TestMethod]
		public void GivenLogIsShorterThanIndexed_WhenReading_ThenReturnsFalse()
		{
			var bytes = Encoding.UTF8.GetBytes("a\nb\nc\n");
			using var saved = new MemoryStream();
			IndexFile.Write(saved, LineIndex.Build(new MemoryStream(bytes)));
			saved.Position = 0;

			IndexFile.TryRead(saved, logLength: 4, out _, out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenTruncatedIndex_WhenReading_ThenReturnsFalse()
		{
			using MemoryStream source = LineIndexTests.ToStream("a\nb\nc\nd\n", new UTF8Encoding(false));
			using var saved = new MemoryStream();
			IndexFile.Write(saved, LineIndex.Build(source, checkpointInterval: 1));
			var truncated = new MemoryStream(saved.ToArray(), 0, (int)saved.Length - 1);

			TryRead(truncated, source, out _, out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenASavedIndexWithAParse_WhenReading_ThenTheParseIsReadBack()
		{
			// After a full parse, the index keeps what parsed the log and the checksums of the bytes read (#1023).
			using MemoryStream source = LineIndexTests.ToStream("a\nb\nc\nd\n", new UTF8Encoding(false));
			SavedParse parse = CreateParse();
			using var saved = new MemoryStream();
			IndexFile.Write(saved, LineIndex.Build(source), parse);
			saved.Position = 0;

			TryRead(saved, source, out _, out IndexFile file).Should().BeTrue();

			file.Parse.IsSameAs(parse).Should().BeTrue();
			file.Parse.PluginName.Should().Be(parse.PluginName);
			file.Parse.Checksums.Should().Equal(parse.Checksums);
			file.HasTable.Should().BeFalse();
		}

		[TestMethod]
		public void GivenAnyByteOfSavedIndexChanged_WhenReading_ThenReturnsFalse()
		{
			// Every section, the parse included, is covered by its checksum, or by the signature and version (#1023).
			using MemoryStream source = LineIndexTests.ToStream("a\nb\nc\nd\n", new UTF8Encoding(false));
			using var saved = new MemoryStream();
			IndexFile.Write(saved, LineIndex.Build(source, checkpointInterval: 1), CreateParse());
			var original = saved.ToArray();
			TryRead(new MemoryStream(original), source, out _, out _).Should().BeTrue("the index is valid before any byte is changed");

			for (var i = 0; i < original.Length; i++)
			{
				var damaged = original.ToArray();
				damaged[i] ^= 0x01;

				TryRead(new MemoryStream(damaged), source, out _, out _).Should().BeFalse($"byte {i} was changed");
			}
		}

		/// <summary>
		/// A parse of the 8 bytes of "a\nb\nc\nd\n": one block, so one checksum.
		/// </summary>
		private static SavedParse CreateParse()
		{
			return new SavedParse("Plugin", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StorageMode.Disk, startOffset: 0, endOffset: 8, checksums: [123]);
		}

		[TestMethod]
		public void GivenFileThatIsNotAnIndex_WhenReading_ThenReturnsFalse()
		{
			using MemoryStream source = LineIndexTests.ToStream("a\nb\n", new UTF8Encoding(false));
			using MemoryStream notAnIndex = LineIndexTests.ToStream("This is not an index file.", new UTF8Encoding(false));

			TryRead(notAnIndex, source, out _, out _).Should().BeFalse();
		}
	}
}
