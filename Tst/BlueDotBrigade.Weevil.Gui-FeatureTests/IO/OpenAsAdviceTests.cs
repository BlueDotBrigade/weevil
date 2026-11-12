namespace BlueDotBrigade.Weevil.Gui.IO
{
	using BlueDotBrigade.Weevil.IO;

	[TestClass]
	public class OpenAsAdviceTests
	{
		private static readonly StorageUnit OneGigabyte = new StorageUnit(StorageUnit.BytesPerGigabyte);

		[TestMethod]
		[DataRow(2, 8)]
		[DataRow(7, 8)]
		public void GivenALogThatFitsInTheFreeMemory_WhenAdvised_ThenRamIsSuggestedWithoutAWarning(double estimateGigabytes, double availableGigabytes)
		{
			// The second row fits exactly: the estimate and the 1 GB reserve take all the free memory.
			var advice = Advise(estimateGigabytes, availableGigabytes);

			advice.DefaultMode.Should().Be(StorageMode.Ram);
			advice.GetWarning(StorageMode.Ram).Should().BeEmpty();
		}

		[TestMethod]
		public void GivenALogThatFitsOnlyWithoutTheReserve_WhenAdvised_ThenDiskIsSuggestedAndRamWarns()
		{
			// 1 GB is kept free for Weevil's window and headroom (decision 2 of the #1028 plan).
			var advice = Advise(estimateGigabytes: 7.5, availableGigabytes: 8);

			advice.DefaultMode.Should().Be(StorageMode.Disk);
			advice.GetWarning(StorageMode.Ram).Should().Contain("may become slow");
			advice.GetWarning(StorageMode.Disk).Should().BeEmpty("the log is on a local drive");
		}

		[TestMethod]
		public void GivenTheFreeMemoryIsUnknown_WhenAdvised_ThenRamIsSuggestedWithoutAWarning()
		{
			// Weevil opened every log in RAM before Open As, so it suggests that when it cannot tell.
			var advice = new OpenAsAdvice("Application.log", OneGigabyte, OneGigabyte, availableMemory: null, isOnNetworkDrive: false);

			advice.DefaultMode.Should().Be(StorageMode.Ram);
			advice.GetWarning(StorageMode.Ram).Should().BeEmpty();
		}

		[TestMethod]
		public void GivenTheLogsMemoryCouldNotBeEstimated_WhenAdvised_ThenRamIsSuggested()
		{
			var advice = new OpenAsAdvice("Application.log", OneGigabyte, estimate: null, new StorageUnit(8 * StorageUnit.BytesPerGigabyte), isOnNetworkDrive: false);

			advice.DefaultMode.Should().Be(StorageMode.Ram);
		}

		[TestMethod]
		public void GivenALogOnANetworkDrive_WhenDiskIsChosen_ThenItWarnsThatFiltersAreSlower()
		{
			var advice = new OpenAsAdvice("Application.log", OneGigabyte, OneGigabyte, new StorageUnit(8 * StorageUnit.BytesPerGigabyte), isOnNetworkDrive: true);

			advice.GetWarning(StorageMode.Disk).Should().Contain("network drive");
			advice.GetWarning(StorageMode.Ram).Should().BeEmpty("RAM reads the log once");
		}

		[TestMethod]
		[DataRow(@"\\Server\Logs\Application.log", true)]
		[DataRow(@"\\?\UNC\Server\Logs\Application.log", true)]
		[DataRow(@"\\?\C:\Logs\Application.log", false)]
		public void GivenAPath_WhenChecked_ThenOnlyAShareIsOnANetworkDrive(string path, bool isOnNetworkDrive)
		{
			OpenAsAdvice.IsNetworkPath(path).Should().Be(isOnNetworkDrive);
		}

		private static OpenAsAdvice Advise(double estimateGigabytes, double availableGigabytes)
		{
			return new OpenAsAdvice(
				"Application.log",
				OneGigabyte,
				new StorageUnit((ulong)(estimateGigabytes * StorageUnit.BytesPerGigabyte)),
				new StorageUnit((ulong)(availableGigabytes * StorageUnit.BytesPerGigabyte)),
				isOnNetworkDrive: false);
		}
	}
}
