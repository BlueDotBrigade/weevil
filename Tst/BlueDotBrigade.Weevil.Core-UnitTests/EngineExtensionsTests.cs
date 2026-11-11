namespace BlueDotBrigade.Weevil
{
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Filter;
	using LogFileChangedException = BlueDotBrigade.Weevil.IO.LogFileChangedException;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	[TestClass]
	public class EngineExtensionsTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Reload reads the log again, so each test opens its own copy.
			_folder = Directory.CreateTempSubdirectory("WeevilEngineExtensions.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenALogOnDiskThatChanged_WhenRunReloadingOnChange_ThenTheWorkRunsAgainOnTheReloadedLog()
		{
			IEngine engine = OpenCopy(StorageMode.Disk);

			// Rewritten with its first 150 lines, as a logger that starts over does.
			File.WriteAllLines(engine.SourceFilePath, File.ReadAllLines(engine.SourceFilePath).Take(150));

			var recordCount = engine.RunReloadingOnChange(current =>
			{
				current.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
				return current.Count;
			});

			recordCount.Should().Be(150);
		}

		[TestMethod]
		public void GivenWorkThatFindsAChangeTwice_WhenRunReloadingOnChange_ThenTheSecondChangeReachesTheCaller()
		{
			IEngine engine = OpenCopy(StorageMode.Ram);
			var runs = 0;

			FluentActions.Invoking(() => engine.RunReloadingOnChange<int>(_ =>
				{
					runs++;
					throw new LogFileChangedException("The log changed.");
				}))
				.Should().Throw<LogFileChangedException>();

			runs.Should().Be(2);
		}

		[TestMethod]
		public void GivenWorkThatFailsForAnotherReason_WhenRunReloadingOnChange_ThenItIsNotRunAgain()
		{
			IEngine engine = OpenCopy(StorageMode.Ram);
			var runs = 0;

			FluentActions.Invoking(() => engine.RunReloadingOnChange<int>(_ =>
				{
					runs++;
					throw new IOException("The log is gone.");
				}))
				.Should().Throw<IOException>();

			runs.Should().Be(1);
		}

		private IEngine OpenCopy(StorageMode storageMode)
		{
			var logPath = Path.Combine(_folder, "SampleData.log");
			File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);

			return Engine.UsingPath(logPath).UsingStorage(storageMode).Open();
		}
	}
}
