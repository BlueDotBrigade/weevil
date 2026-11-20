namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// The part of the log the status bar shows, so the analyst doesn't mistake a partial open for the whole log (#1219).
	/// </summary>
	[TestClass]
	public class SourceFileOpenedBulletinTests
	{
		[TestMethod]
		public void GivenTheWholeLogOpen_WhenThePartIsShown_ThenNothingIsShown()
		{
			Opened(LogPart.Whole).PartName.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenARegionOpen_WhenThePartIsShown_ThenTheRegionIsNamed()
		{
			Opened(LogPart.ForRegion("Shutdown", new Range(350, 399))).PartName.Should().Be("Region: Shutdown");
		}

		[TestMethod]
		public void GivenLinesFromALineOpen_WhenThePartIsShown_ThenTheFirstLineIsNamed()
		{
			Opened(LogPart.ForLines(new Range(1200, int.MaxValue))).PartName.Should().Be($"From line {1200:N0}", "the status bar shows numbers as the analyst's culture does");
		}

		[TestMethod]
		public void GivenARangeOfLinesOpen_WhenThePartIsShown_ThenItsFirstAndLastLinesAreNamed()
		{
			Opened(LogPart.ForLines(new Range(10, 1500))).PartName.Should().Be($"Lines 10 – {1500:N0}");
		}

		private static SourceFileOpenedBulletin Opened(LogPart part)
		{
			return new SourceFileOpenedBulletin
			{
				SourceFilePath = @"C:\Logs\Droid.log",
				Part = part,
			};
		}
	}
}
