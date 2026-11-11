namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class MemoryEstimatorTests
	{
		private const int BytesPerRecord = MemoryEstimator.BytesPerRecord;

		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = System.IO.Directory.CreateTempSubdirectory("WeevilEstimate.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenASmallLogWithoutAnIndex_WhenEstimated_ThenItIsTheLogsSizePlusEachLine()
		{
			var logPath = WriteLog("a\nbb\nccc");

			MemoryEstimator.Estimate(logPath).Bytes.Should().Be(8 + 3 * BytesPerRecord);
		}

		[TestMethod]
		public void GivenALogLargerThanTheSampleWithoutAnIndex_WhenEstimated_ThenTheLinesAreCountedFromTheSample()
		{
			// 3 MB of 100-byte lines: the first megabyte holds about a third of them.
			var line = new string('x', 99) + "\n";
			var logPath = WriteLog(string.Concat(Enumerable.Repeat(line, 3 * 1024 * 1024 / 100)));
			var length = new FileInfo(logPath).Length;
			var lines = length / 100;

			MemoryEstimator.Estimate(logPath).Bytes
				.Should().BeCloseTo((ulong)(length + lines * BytesPerRecord), (ulong)(lines * BytesPerRecord / 100));
		}

		[TestMethod]
		public void GivenALogWithAnIndex_WhenEstimated_ThenTheIndexsLineCountIsUsed()
		{
			// The index was saved for content with 4 lines; the log now holds as many bytes, on one line.
			var logPath = WriteLog("a\nb\nc\nd\n");
			SaveIndex(logPath);
			System.IO.File.WriteAllText(logPath, "abcdefgh");

			MemoryEstimator.Estimate(logPath).Bytes.Should().Be(8 + 4 * BytesPerRecord);
		}

		[TestMethod]
		public void GivenALogThatGrewPastItsIndex_WhenEstimated_ThenTheIndexsLineCountIsScaledUp()
		{
			var logPath = WriteLog("a\nb\nc\nd\n");
			SaveIndex(logPath);
			System.IO.File.AppendAllText(logPath, "e\nf\ng\nh\n");

			MemoryEstimator.Estimate(logPath).Bytes.Should().Be(16 + 8 * BytesPerRecord);
		}

		[TestMethod]
		public void GivenALogShorterThanItsIndex_WhenEstimated_ThenTheLinesAreCountedFromTheLog()
		{
			// The index belongs to a longer log, since replaced.
			var logPath = WriteLog("a\nb\nc\nd\ne\nf\ng\nh\n");
			SaveIndex(logPath);
			System.IO.File.WriteAllText(logPath, "a\nb\n");

			MemoryEstimator.Estimate(logPath).Bytes.Should().Be(4 + 2 * BytesPerRecord);
		}

		[TestMethod]
		public void GivenAnEmptyLog_WhenEstimated_ThenItNeedsNoMemory()
		{
			var logPath = WriteLog(string.Empty);

			MemoryEstimator.Estimate(logPath).Should().Be(StorageUnit.Zero);
		}

		private string WriteLog(string content)
		{
			var logPath = Path.Combine(_folder, $"{Guid.NewGuid():N}.log");
			System.IO.File.WriteAllText(logPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			return logPath;
		}

		/// <remarks>
		/// Written directly: <see cref="LineIndexFile"/> saves no index for a log under the temporary folder.
		/// </remarks>
		private static void SaveIndex(string logPath)
		{
			using var log = System.IO.File.OpenRead(logPath);
			using var index = System.IO.File.Create($"{logPath}.{LineIndexFile.FileExtension}");
			LineIndex.Build(log).Write(index);
		}
	}
}
