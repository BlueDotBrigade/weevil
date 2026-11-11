namespace BlueDotBrigade.Weevil.PowerShell
{
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	[TestClass]
	public class GetRecordsCmdletTests
	{
		// Regression: Issue #1002
		[TestMethod]
		public void GivenIncludeFilter_WhenGettingRecords_ThenReturnsOneRecordPerMatch()
		{
			var cmdlet = new GetRecordsCmdlet
			{
				FilePath = new Daten().AsFilePath("Application.log"),
				Include = "Error",
			};

			var output = cmdlet.Invoke().Cast<object>().ToList();

			output.Should().AllBeAssignableTo<IRecord>();
			output.Cast<IRecord>().Select(record => record.LineNumber).Should().Equal(3, 5, 8);
		}

		// Regression: Issue #1002
		[TestMethod]
		public void GivenIncludeAndExcludeFilters_WhenGettingRecords_ThenReturnsOneRecordPerMatch()
		{
			var cmdlet = new GetRecordsCmdlet
			{
				FilePath = new Daten().AsFilePath("Application.log"),
				Include = "database",
				Exclude = "Unable",
			};

			var output = cmdlet.Invoke().Cast<object>().ToList();

			output.Should().AllBeAssignableTo<IRecord>();
			output.Cast<IRecord>().Select(record => record.LineNumber).Should().Equal(2, 6);
		}

		[TestMethod]
		[DataRow(StorageMode.Ram, false)]
		[DataRow(StorageMode.Disk, true)]
		public void GivenAStorageMode_WhenTheLogIsTruncatedAfterGettingRecords_ThenOnlyRecordsOnDiskFailToRead(StorageMode storage, bool readFails)
		{
			// On disk, a record reads its content when the pipeline uses it, so the help warns that a log changed before then fails the read (#1028).
			var folder = Directory.CreateTempSubdirectory("WeevilPowerShell.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Application.log");
				File.Copy(new Daten().AsFilePath("Application.log"), logPath);
				var cmdlet = new GetRecordsCmdlet
				{
					FilePath = logPath,
					Include = "Error",
					Storage = storage,
				};
				var records = cmdlet.Invoke().Cast<IRecord>().ToList();

				File.WriteAllText(logPath, string.Empty);

				var read = FluentActions.Invoking(() => records[0].Content);

				if (readFails)
				{
					read.Should().Throw<IOException>("a record on disk reads the log again");
				}
				else
				{
					read.Should().NotThrow("a record in RAM keeps its content");
				}
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}
}
