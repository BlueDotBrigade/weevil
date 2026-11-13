namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class SavedTableReaderTests
	{
		private string _folder = string.Empty;
		private string _logPath = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = System.IO.Directory.CreateTempSubdirectory("Weevil.SavedTableReaderTests.").FullName;
			_logPath = Path.Combine(_folder, "Log.txt");
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[DataRow(StorageMode.Ram)]
		[DataRow(StorageMode.Disk)]
		public void GivenATableWithTextAParserChanged_WhenSavedAndReadBack_ThenEveryRecordIsTheSame(StorageMode storageMode)
		{
			// Text a plugin's parser changed is not in the log, so the saved index keeps it, while the other rows keep their place in the log (#1023).
			var lines = Enumerable.Range(1, 3_000).Select(i => i % 7 == 0 ? $"change {i}" : i % 11 == 0 ? $" continued {i}" : $"keep {i}");
			System.IO.File.WriteAllText(_logPath, string.Join("\n", lines) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

			using var log = new System.IO.FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var repository = new SerializedRecordRepository(log, new ChangingParser(), MultilineRecordParser.MaximumLinesToSearch, storageMode);
			var parsed = (TableRecordList)repository.Get(Range.All, int.MaxValue);
			LogFileSource parsedSource = repository.ParsedSource;
			var parse = new SavedParse("Plugin", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), storageMode, parsedSource.StartOffset, parsedSource.EndOffset, parsedSource.GetChecksums().ToArray());

			LineIndex index = LineIndex.Build(log);
			index.Parse = parse;
			using var saved = new MemoryStream();
			index.Write(saved, parsed.WholeTable);
			saved.Position = 0;
			LineIndex.TryRead(saved, log, out LineIndex read).Should().BeTrue();
			saved.Position = read.TableOffset;

			SavedTableReader.TryRead(saved, log, _logPath, read.Parse, read.ContentEncoding, storageMode, out RecordTable table, out LogFileSource source).Should().BeTrue();

			var reopened = new TableRecordList(table);
			Describe(reopened).Should().Equal(Describe(parsed));
			Describe(reopened).Should().Contain(row => row.Contains("CHANGE 7"), "the parser's text is kept");
			(source != null).Should().Be(storageMode == StorageMode.Disk, "only disk mode reads content back from the log");
		}

		private static string[] Describe(RecordList records)
		{
			return records.Select(r => $"{r.LineNumber}|{r.CreatedAt:O}|{r.Severity}|{r.IsMultiLine}|{r.WasGeneratedByUi}|{r.Content}").ToArray();
		}

		/// <summary>
		/// Upper-cases the lines that start with "change", so their records keep a string. Lines that start with a space continue the record before them.
		/// </summary>
		private sealed class ChangingParser : IRecordParser
		{
			public bool TryParse(int line, string content, out IRecord record)
			{
				if (content.StartsWith(' '))
				{
					record = Record.Dummy;
					return false;
				}

				var isChanged = content.StartsWith("change", StringComparison.Ordinal);
				record = new Record(line, new DateTime(2026, 11, 13).AddSeconds(line), SeverityType.Information, isChanged ? content.ToUpperInvariant() : content, isMultiLine: false, wasGeneratedByUi: isChanged);
				return true;
			}
		}
	}
}
