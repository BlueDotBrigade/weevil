namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	[TestClass]
	public class FileTypeFilterTests
	{
		private static readonly FileTypeFilter LogFiles = new FileTypeFilter("Log files", new[] { "*.log", "*.log.*", "*.txt" }, new[] { "*.xml", "*.idx" });

		[TestMethod]
		[DataRow("App.log", true)]
		[DataRow("App.log.1", true)]
		[DataRow("Notes.txt", true)]
		[DataRow("App.log.xml", false)]
		[DataRow("App.log.idx", false)]
		[DataRow("Picture.png", false)]
		public void GivenLogFiles_WhenAFileIsMatched_ThenRolledLogsAreShownAndSidecarsAndIndexesAreHidden(string fileName, bool isShown)
		{
			LogFiles.Matches(fileName).Should().Be(isShown);
		}

		[TestMethod]
		public void GivenAFilterInAnotherCase_WhenAFileIsMatched_ThenCaseIsIgnored()
		{
			LogFiles.Matches("APP.LOG").Should().BeTrue();
		}

		[TestMethod]
		public void GivenAllFiles_WhenAnyFileIsMatched_ThenItIsShown()
		{
			FileTypeFilter.AllFiles.Matches("App.log.xml").Should().BeTrue();
			FileTypeFilter.AllFiles.Matches("anything").Should().BeTrue();
		}
	}
}
