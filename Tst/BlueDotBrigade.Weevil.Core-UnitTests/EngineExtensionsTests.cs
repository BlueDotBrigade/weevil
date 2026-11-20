namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
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
			// Each test renames its own copy of the log.
			_folder = Directory.CreateTempSubdirectory("WeevilEngineExtensions.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[WorkItem(1247)]
		public void GivenALogOnDiskRenamedBeforeItIsFiltered_WhenRunOnce_ThenTheErrorSaysToRunTheCommandAgain()
		{
			// The CLI and PowerShell reloaded a changed log and ran the command again, rather than leaving that to the analyst.
			using IEngine engine = OpenCopy(StorageMode.Disk);
			File.Move(engine.SourceFilePath, engine.SourceFilePath + ".1");
			var runs = 0;

			FluentActions.Invoking(() => engine.RunOnce(
					() =>
					{
						runs++;
						return engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
					},
					_ => { }))
				.Should().Throw<LogFileChangedException>()
				.WithMessage("*renamed, deleted or replaced*Run the command again.");

			runs.Should().Be(1);
		}

		[TestMethod]
		[WorkItem(1247)]
		public void GivenALogInRamRenamedBeforeItIsFiltered_WhenRunOnce_ThenTheResultIsKeptWithAWarning()
		{
			// The analyst was not told that the log the results came from had been renamed.
			using IEngine engine = OpenCopy(StorageMode.Ram);
			File.Move(engine.SourceFilePath, engine.SourceFilePath + ".1");
			var warnings = new List<string>();

			var shown = engine.RunOnce(() => engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1")).Results.Count, warnings.Add);

			shown.Should().BeGreaterThan(0);
			warnings.Should().ContainSingle().Which.Should().Contain("renamed, deleted or replaced");
		}

		[TestMethod]
		public void GivenALogThatIsUnchangedOrOnlyGrew_WhenRunOnce_ThenNoWarningIsGiven()
		{
			// A live log grows while it is read, which is no reason to warn.
			using IEngine engine = OpenCopy(StorageMode.Ram);
			var warnings = new List<string>();

			engine.RunOnce(() => engine.Count, warnings.Add);
			File.AppendAllText(engine.SourceFilePath, "2026-11-18 10:00:00.000 Information Id=999 One more line" + System.Environment.NewLine);
			engine.RunOnce(() => engine.Count, warnings.Add);

			warnings.Should().BeEmpty();
		}

		[TestMethod]
		[WorkItem(1247)]
		public void GivenAResultThatStillReadsTheLog_WhenTheLogIsRenamedAsTheWorkFinishes_ThenTheErrorSaysToRunTheCommandAgain()
		{
			// Get-Records on disk warned, then returned records whose text could no longer be read.
			using IEngine engine = OpenCopy(StorageMode.Ram);

			FluentActions.Invoking(() => engine.RunOnce(() =>
				{
					File.Move(engine.SourceFilePath, engine.SourceFilePath + ".1");
					return engine.Count;
				}))
				.Should().Throw<LogFileChangedException>()
				.WithMessage("*Run the command again.");
		}

		[TestMethod]
		public void GivenWorkThatFailsForAnotherReason_WhenRunOnce_ThenItsExceptionReachesTheCaller()
		{
			using IEngine engine = OpenCopy(StorageMode.Ram);
			var failure = new IOException("The destination is full.");

			FluentActions.Invoking(() => engine.RunOnce<int>(() => throw failure, _ => { }))
				.Should().Throw<IOException>().Which.Should().BeSameAs(failure);
		}

		private IEngine OpenCopy(StorageMode storageMode)
		{
			var logPath = Path.Combine(_folder, "SampleData.log");
			File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);

			return Engine.UsingPath(logPath).UsingStorage(storageMode).Open();
		}
	}
}
