namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using NSubstitute;

	[TestClass]
	public class SavedIndexTests
	{
		private string _directory;
		private string _logFilePath;
		private string _indexFilePath;

		[TestInitialize]
		public void Setup()
		{
			// Not under the temporary folder: files there are treated as extracted from a compressed archive, and the index is not saved.
			_directory = Path.Combine(AppContext.BaseDirectory, nameof(SavedIndexTests), Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(_directory);

			_logFilePath = Path.Combine(_directory, "Application.log");
			_indexFilePath = _logFilePath + ".idx";
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_directory, recursive: true);
		}

		private static LineIndex Load(string logFilePath)
		{
			using var logSource = new System.IO.FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			return SavedIndex.Load(logFilePath, logSource).LineIndex;
		}

		private static RecordList ReadAllRecords(System.IO.FileStream log, string logFilePath)
		{
			var plugin = new DefaultPlugin();

			return SavedIndex.Load(logFilePath, log).ReadAllRecords(log, plugin, plugin.GetExtension(logFilePath), StorageMode.Ram, displayPath: null, out _);
		}

		[TestMethod]
		public void GivenNoSavedIndex_WhenLoading_ThenIndexIsSavedBesideLog()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");

			LineIndex index = Load(_logFilePath);

			index.LineCount.Should().Be(3);
			System.IO.File.Exists(_indexFilePath).Should().BeTrue();
			System.IO.File.Exists(_indexFilePath + ".tmp").Should().BeFalse();
		}

		[TestMethod]
		[DoNotParallelize]
		[DataRow(0L, DisplayName = "While the log is matched to its line index")]
		[DataRow((long)LogFileSource.BlockSize, DisplayName = "While the log's blocks are checked")]
		public void GivenTheLogFailsToBeReadWhileItsSavedTableIsChecked_WhenItsRecordsAreRead_ThenTheLogIsNamedAndTheSavedTableIsKept(long failingOffset)
		{
			// The log, not the saved index, failed to be read, so the table saved with the index may still be the log's (#1256).
			// Longer than two blocks, so only checking the blocks reads from the second block's start: the line index reads the log's first and last few KB.
			const int LineCount = 20_000;
			var lines = new StringBuilder();
			for (var line = 0; line < LineCount; line++)
			{
				lines.Append($"Line {line:D5}\n");
			}

			System.IO.File.WriteAllText(_logFilePath, lines.ToString());
			using (var firstOpen = new System.IO.FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				ReadAllRecords(firstOpen, _logFilePath);
			}

			var savedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			System.IO.File.SetLastWriteTimeUtc(_indexFilePath, savedAt);
			ILogWriter original = Log.Default;
			ILogWriter log = Substitute.For<ILogWriter>();

			RecordList records;
			bool hasFailed;
			try
			{
				Log.Register(log);
				using var reopened = new FailingOnceFileStream(_logFilePath);
				var plugin = new DefaultPlugin();
				var savedIndex = SavedIndex.Load(_logFilePath, reopened);
				reopened.FailingOffset = failingOffset;

				records = savedIndex.ReadAllRecords(reopened, plugin, plugin.GetExtension(_logFilePath), StorageMode.Ram, displayPath: null, out _);
				hasFailed = reopened.HasFailed;
			}
			finally
			{
				Log.Register(original);
			}

			hasFailed.Should().BeTrue("the read at the failing offset should have been tried");
			records.Count.Should().Be(LineCount, "the log was parsed");
			log.Received().Write(
				LogSeverityType.Warning,
				Arg.Any<IOException>(),
				Arg.Is<string>(message => message.StartsWith(SavedIndex.LogUnreadableMessage, StringComparison.Ordinal) && message.Contains(_logFilePath, StringComparison.Ordinal)));
			System.IO.File.GetLastWriteTimeUtc(_indexFilePath).Should().Be(savedAt, "the parse made the same table, so the saved one was kept");
		}

		[TestMethod]
		[WorkItem(1323)]
		[DoNotParallelize]
		[DataRow(false, false, DisplayName = "While its content is read, on a partial open")]
		[DataRow(false, true, DisplayName = "While its content is read, on a full open")]
		[DataRow(true, false, DisplayName = "While its length is read")]
		public void GivenTheLogFailsToBeReadWhileItsSavedLineIndexIsChecked_WhenOpening_ThenTheLogIsNamedInAWarningAndTheSavedIndexIsKept(bool failsLength, bool isFullOpen)
		{
			// A failed read of the log, as after a brief network drop, was blamed on the saved index, which was then overwritten without its record table.
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			using (var firstOpen = new System.IO.FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				ReadAllRecords(firstOpen, _logFilePath);
			}

			var savedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			System.IO.File.SetLastWriteTimeUtc(_indexFilePath, savedAt);
			ILogWriter original = Log.Default;
			ILogWriter log = Substitute.For<ILogWriter>();

			LineIndex index;
			bool hasFailed;
			try
			{
				Log.Register(log);
				using var reopened = new FailingOnceFileStream(_logFilePath);
				if (failsLength)
				{
					reopened.FailsLength = true;
				}
				else
				{
					reopened.FailingOffset = 0;
				}

				var savedIndex = SavedIndex.Load(_logFilePath, reopened);
				index = savedIndex.LineIndex;
				hasFailed = reopened.HasFailed;

				if (isFullOpen)
				{
					// The log reads again by now, so the saved record table is checked and reused.
					var plugin = new DefaultPlugin();
					savedIndex.ReadAllRecords(reopened, plugin, plugin.GetExtension(_logFilePath), StorageMode.Ram, displayPath: null, out _).Count.Should().Be(3);
				}
			}
			finally
			{
				Log.Register(original);
			}

			hasFailed.Should().BeTrue("the failing read of the log should have been tried");
			index.LineCount.Should().Be(3, "the line index was rebuilt from the log");
			log.Received().Write(
				LogSeverityType.Warning,
				Arg.Any<IOException>(),
				Arg.Is<string>(message => message.StartsWith(SavedIndex.LogUnreadableForLineIndexMessage, StringComparison.Ordinal) && message.Contains(_logFilePath, StringComparison.Ordinal)));
			System.IO.File.GetLastWriteTimeUtc(_indexFilePath).Should().Be(savedAt, "the saved index may still match the log, so it was not overwritten");
			if (isFullOpen)
			{
				log.Received().Write(LogSeverityType.Information, SavedIndex.RecordsReadFromIndexMessage, Arg.Any<IEnumerable<KeyValuePair<string, object>>>());
			}
		}

		[TestMethod]
		public void GivenSavedIndexMatchesLog_WhenLoading_ThenSavedIndexIsReused()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			Load(_logFilePath);
			var savedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			System.IO.File.SetLastWriteTimeUtc(_indexFilePath, savedAt);

			LineIndex index = Load(_logFilePath);

			index.LineCount.Should().Be(3);
			System.IO.File.GetLastWriteTimeUtc(_indexFilePath).Should().Be(savedAt);
		}

		[TestMethod]
		public void GivenLogHasGrown_WhenLoading_ThenNewLinesAreIndexedAndSaved()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\n");
			Load(_logFilePath);
			System.IO.File.AppendAllText(_logFilePath, "c\nd\n");

			LineIndex index = Load(_logFilePath);

			index.LineCount.Should().Be(4);
			using FileStream logSource = System.IO.File.OpenRead(_logFilePath);
			using FileStream indexSource = System.IO.File.OpenRead(_indexFilePath);
			IndexFile.TryRead(indexSource, logSource.Length, out LineIndex saved, out _).Should().BeTrue();
			saved.LineCount.Should().Be(4);
		}

		[TestMethod]
		public void GivenLogWasReplaced_WhenLoading_ThenIndexIsRebuilt()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			Load(_logFilePath);
			System.IO.File.WriteAllText(_logFilePath, "x\ny\n");

			Load(_logFilePath).LineCount.Should().Be(2);
		}

		[TestMethod]
		public void GivenSavedIndexIsCorrupt_WhenLoading_ThenIndexIsRebuilt()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			System.IO.File.WriteAllText(_indexFilePath, "Not an index.");

			Load(_logFilePath).LineCount.Should().Be(3);
		}

		[TestMethod]
		public void GivenIndexCannotBeSaved_WhenLoading_ThenLoadingContinues()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			// A folder where the temporary index file would be written makes saving fail.
			System.IO.Directory.CreateDirectory(_indexFilePath + ".tmp");

			LineIndex index = Load(_logFilePath);

			index.LineCount.Should().Be(3);
			System.IO.File.Exists(_indexFilePath).Should().BeFalse();
		}

		[TestMethod]
		public void GivenLogFromCompressedArchive_WhenLoading_ThenIndexIsNotSaved()
		{
			var extractedDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(extractedDirectory);

			try
			{
				var extractedLogFilePath = Path.Combine(extractedDirectory, "Application.log");
				System.IO.File.WriteAllText(extractedLogFilePath, "a\nb\nc\n", Encoding.UTF8);

				LineIndex index = Load(extractedLogFilePath);

				index.LineCount.Should().Be(3);
				System.IO.File.Exists(extractedLogFilePath + ".idx").Should().BeFalse();
			}
			finally
			{
				System.IO.Directory.Delete(extractedDirectory, recursive: true);
			}
		}

		/// <summary>
		/// A log whose first read from <see cref="FailingOffset"/>, or first read of its length, fails, as a file on a network share might.
		/// </summary>
		private sealed class FailingOnceFileStream(string path) : System.IO.FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
		{
			public long? FailingOffset { get; set; }

			public bool FailsLength { get; set; }

			public bool HasFailed { get; private set; }

			public override long Length
			{
				get
				{
					if (this.FailsLength)
					{
						this.FailsLength = false;
						this.HasFailed = true;
						throw new IOException("The network path was not found.");
					}

					return base.Length;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				FailIfAsked();
				return base.Read(buffer, offset, count);
			}

			public override int Read(Span<byte> buffer)
			{
				FailIfAsked();
				return base.Read(buffer);
			}

			public override int ReadByte()
			{
				FailIfAsked();
				return base.ReadByte();
			}

			private void FailIfAsked()
			{
				if (this.Position == this.FailingOffset)
				{
					this.FailingOffset = null;
					this.HasFailed = true;
					throw new IOException("The network path was not found.");
				}
			}
		}
	}
}
