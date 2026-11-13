namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	[TestClass]
	public class SerializedRecordRepositoryTests
	{
		private const int PreambleLineCount = 300;

		/// <summary>
		/// Creates a TSV log whose first record follows more lines than the parser searches by default.
		/// </summary>
		private static MemoryStream CreateLogWithLongPreamble()
		{
			var preamble = Enumerable
				.Range(1, PreambleLineCount)
				.Select(lineNumber => $"Preamble line {lineNumber}");

			var records = Enumerable
				.Range(PreambleLineCount + 1, 3)
				.Select(lineNumber => $"2026-10-16 10:00:00.00000\t1\t2\tInformation\tRecord {lineNumber}");

			var content = string.Join("\n", preamble.Concat(records));

			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}

		[TestMethod]
		public void GivenFirstRecordBeyondDefaultSearchLimit_WhenGettingRecords_ThenNoRecordsAreReturned()
		{
			var repository = new SerializedRecordRepository(CreateLogWithLongPreamble(), new TsvRecordParser());

			var records = repository.Get(new Range(PreambleLineCount + 1, int.MaxValue), int.MaxValue);

			records.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenFirstRecordBeyondDefaultSearchLimit_WhenSearchLimitReachesIt_ThenRecordsAreReturned()
		{
			var repository = new SerializedRecordRepository(
				CreateLogWithLongPreamble(),
				new TsvRecordParser(),
				PreambleLineCount + MultilineRecordParser.MaximumLinesToSearch);

			var records = repository.Get(new Range(PreambleLineCount + 1, int.MaxValue), int.MaxValue);

			records.Should().HaveCount(3);
			records[0].LineNumber.Should().Be(PreambleLineCount + 1);
			records[0].Content.Should().EndWith($"Record {PreambleLineCount + 1}");
		}

		// Issue #977: the byte order mark is only at the top of the file, so reading from an offset must still decode UTF-16.
		[TestMethod]
		[DataRow(false, DisplayName = "UTF-16 little-endian")]
		[DataRow(true, DisplayName = "UTF-16 big-endian")]
		public void GivenUtf16Log_WhenReadingFromLineOffset_ThenContentIsDecoded(bool isBigEndian)
		{
			var encoding = new UnicodeEncoding(isBigEndian, byteOrderMark: true);
			const string firstLine = "2026-10-16 10:00:00.00000\t1\t2\tInformation\tRecord 1\n";
			const string secondLine = "2026-10-16 10:00:01.00000\t1\t2\tInformation\tRecord 2 \u00e9\n";
			var source = new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(firstLine + secondLine)).ToArray());
			var secondLineOffset = encoding.GetPreamble().Length + encoding.GetByteCount(firstLine);

			var repository = new SerializedRecordRepository(source, new TsvRecordParser(), secondLineOffset, 2, true);
			var records = repository.Get(int.MaxValue);

			records.Should().HaveCount(1);
			records[0].LineNumber.Should().Be(2);
			records[0].Content.Should().Be(secondLine.TrimEnd('\n'));
		}

		[TestMethod]
		public void GivenAParserThatChangesTheTextOfSomeLines_WhenRecordsAreRead_ThenRecordsThatKeepStringsAndBytesAreBothRight()
		{
			// Changed lines keep a string, and the others keep the log's bytes, so both go through the record table together (#1138).
			var log = new MemoryStream(Encoding.UTF8.GetBytes("keep 1\nchange 2\n  more\nkeep 4\n  more\nchange 6\nkeep 7"));
			var repository = new SerializedRecordRepository(log, new ChangingParser());

			var records = repository.Get(Range.All, int.MaxValue);

			records.Select(r => r.LineNumber).Should().Equal(1, 2, 4, 6, 7);
			records.Select(r => r.Content).Should().Equal("keep 1", "CHANGE 2\r\n  more", "keep 4\r\n  more", "CHANGE 6", "keep 7");
			records.Select(r => r.IsMultiLine).Should().Equal(false, true, true, false, false);
			records.Select(r => r.WasGeneratedByUi).Should().Equal(false, true, false, true, false);
		}

		[TestMethod]
		[DataRow("UTF-16BE")]
		[DataRow("UTF-32LE")]
		public void GivenALogOnDiskAndAParserThatChangesSomeLines_WhenRecordsAreRead_ThenOnlyTheChangedTextIsKeptInMemory(string encodingName)
		{
			// In RAM mode these encodings keep a string per record, because they decode slowly. On disk, every record whose text
			// is the log's own is read back from the log, and only text the parser changed, which is not in the log, stays in memory (#1025, decision 6).
			Encoding encoding = encodingName == "UTF-16BE"
				? new UnicodeEncoding(bigEndian: true, byteOrderMark: true)
				: new UTF32Encoding(bigEndian: false, byteOrderMark: true);
			var folder = Directory.CreateTempSubdirectory("Weevil.SerializedRecordRepositoryTests.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Log.txt");
				File.WriteAllBytes(logPath, encoding.GetPreamble().Concat(encoding.GetBytes("keep 1\nchange 2\nkeep 3")).ToArray());

				RecordList records;
				using (var log = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					records = new SerializedRecordRepository(log, new ChangingParser(), MultilineRecordParser.MaximumLinesToSearch, StorageMode.Disk)
						.Get(Range.All, int.MaxValue);
				}

				File.Move(logPath, $"{logPath}~");

				records[1].Content.Should().Be("CHANGE 2", "text the parser changed is not in the log, so it stays in memory");
				FluentActions.Invoking(() => records[0].Content).Should().Throw<IOException>("the log's own text is read back from the log");
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		public void GivenALogFile_WhenParsedInRam_ThenItHasTheChecksumsOfAParseOnDiskButNoSourceToReadBack()
		{
			// The saved index keeps the checksums, so a log opened in either mode can be reopened from it (#1023).
			// In RAM mode the records keep their content, so the engine still gets no source: a source means disk mode.
			var folder = Directory.CreateTempSubdirectory("Weevil.SerializedRecordRepositoryTests.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Log.txt");
				File.WriteAllText(logPath, string.Concat(Enumerable.Range(1, 20_000).Select(i => $"Line {i}\r\n")), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

				SerializedRecordRepository inRam = Parse(logPath, StorageMode.Ram);
				SerializedRecordRepository onDisk = Parse(logPath, StorageMode.Disk);

				inRam.Source.Should().BeNull();
				inRam.ParsedSource.GetChecksums().Length.Should().BeGreaterThan(1, "the log spans several blocks");
				inRam.ParsedSource.GetChecksums().ToArray().Should().Equal(onDisk.ParsedSource.GetChecksums().ToArray());
				inRam.ParsedSource.EndOffset.Should().Be(onDisk.ParsedSource.EndOffset);
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}

			static SerializedRecordRepository Parse(string logPath, StorageMode storageMode)
			{
				using var log = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var repository = new SerializedRecordRepository(log, new DefaultRecordParser(), MultilineRecordParser.MaximumLinesToSearch, storageMode);
				_ = repository.Get(Range.All, int.MaxValue);

				return repository;
			}
		}

		/// <summary>
		/// Upper-cases the lines that start with "change", and marks them as written by the user interface thread.
		/// Lines that start with a space continue the record before them.
		/// </summary>
		private sealed class ChangingParser : IRecordParser
		{
			public bool TryParse(int line, string content, out IRecord record)
			{
				var isChanged = content.StartsWith("change", StringComparison.Ordinal);

				record = content.StartsWith(' ')
					? null
					: new Record(line, Record.CreationTimeUnknown, SeverityType.Information, isChanged ? content.ToUpperInvariant() : content, isMultiLine: false, wasGeneratedByUi: isChanged);

				return record != null;
			}
		}
	}
}
