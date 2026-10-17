namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Text;

	[TestClass]
	public class LineIndexFileTests
	{
		private string _directory;
		private string _logFilePath;
		private string _indexFilePath;

		[TestInitialize]
		public void Setup()
		{
			// Not under the temporary folder: files there are treated as extracted from a compressed archive, and the index is not saved.
			_directory = Path.Combine(AppContext.BaseDirectory, nameof(LineIndexFileTests), Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(_directory);

			_logFilePath = Path.Combine(_directory, "Application.log");
			_indexFilePath = _logFilePath + ".idx";
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_directory, recursive: true);
		}

		private LineIndex Load(string logFilePath)
		{
			using var logSource = new System.IO.FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			return LineIndexFile.Load(logFilePath, logSource);
		}

		[TestMethod]
		public void GivenNoSavedIndex_WhenLoading_ThenIndexIsSavedBesideLog()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");

			var index = Load(_logFilePath);

			index.LineCount.Should().Be(3);
			System.IO.File.Exists(_indexFilePath).Should().BeTrue();
			System.IO.File.Exists(_indexFilePath + ".tmp").Should().BeFalse();
		}

		[TestMethod]
		public void GivenSavedIndexMatchesLog_WhenLoading_ThenSavedIndexIsReused()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\nc\n");
			Load(_logFilePath);
			var savedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			System.IO.File.SetLastWriteTimeUtc(_indexFilePath, savedAt);

			var index = Load(_logFilePath);

			index.LineCount.Should().Be(3);
			System.IO.File.GetLastWriteTimeUtc(_indexFilePath).Should().Be(savedAt);
		}

		[TestMethod]
		public void GivenLogHasGrown_WhenLoading_ThenNewLinesAreIndexedAndSaved()
		{
			System.IO.File.WriteAllText(_logFilePath, "a\nb\n");
			Load(_logFilePath);
			System.IO.File.AppendAllText(_logFilePath, "c\nd\n");

			var index = Load(_logFilePath);

			index.LineCount.Should().Be(4);
			using var logSource = System.IO.File.OpenRead(_logFilePath);
			using var indexSource = System.IO.File.OpenRead(_indexFilePath);
			LineIndex.TryRead(indexSource, logSource, out LineIndex saved).Should().BeTrue();
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

			var index = Load(_logFilePath);

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

				var index = Load(extractedLogFilePath);

				index.LineCount.Should().Be(3);
				System.IO.File.Exists(extractedLogFilePath + ".idx").Should().BeFalse();
			}
			finally
			{
				System.IO.Directory.Delete(extractedDirectory, recursive: true);
			}
		}
	}
}
