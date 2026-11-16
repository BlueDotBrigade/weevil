namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	[TestClass]
	public class LocationTests
	{
		private static readonly string ZipPath = Path.Combine(Path.GetTempPath(), "Logs", "Bundle.zip");

		[TestMethod]
		public void GivenAFileInAZipInAZip_WhenShown_ThenItIsAPathThroughBothZips()
		{
			Location.ForArchive(ZipPath, "Nested/Inner.zip", "Old.log").ToString()
				.Should().Be(Path.Combine(ZipPath, "Nested", "Inner.zip", "Old.log"));
		}

		[TestMethod]
		public void GivenAFileInAZipInAZip_WhenGoingUp_ThenItClimbsOutOfBothZipsIntoTheFolder()
		{
			var parents = new List<string>();

			for (Location? location = Location.ForArchive(ZipPath, "Nested/Inner.zip", "Old.log").GetParent(); parents.Count < 4; location = location.GetParent())
			{
				parents.Add(location!.ToString());
			}

			parents.Should().Equal(
				Path.Combine(ZipPath, "Nested", "Inner.zip"),
				Path.Combine(ZipPath, "Nested"),
				ZipPath,
				Path.GetDirectoryName(ZipPath));
		}

		[TestMethod]
		public void GivenFilesAndZips_WhenNamed_ThenTheLastPartIsTheName()
		{
			Location.ForArchive(ZipPath, "Nested/Inner.zip", "Old.log").Name.Should().Be("Old.log");
			Location.ForArchive(ZipPath).Name.Should().Be("Bundle.zip");
		}

		[TestMethod]
		public void GivenAPastedPathInQuotes_WhenParsed_ThenTheQuotesAreIgnored()
		{
			var folder = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

			Location.Parse($"\"{folder}\"", IsZip).Should().Be(Location.ForFileSystem(folder));
		}

		[TestMethod]
		public void GivenAPathThatNamesNothing_WhenParsed_ThenNothingIsFound()
		{
			Location.Parse(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "x.log"), IsZip).Should().BeNull();
		}

		[TestMethod]
		public void GivenTwoPathsThatDifferOnlyInCase_WhenCompared_ThenTheyAreEqual()
		{
			Location.ForArchive(ZipPath, "Nested").Should().Be(Location.ForArchive(ZipPath.ToUpperInvariant(), "NESTED"));
		}

		private static bool IsZip(string name) => name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
	}
}
