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

		[TestMethod]
		public void GivenATypedPathThroughAZipInAZip_WhenParsed_ThenItIsTheFileInTheInnerZip()
		{
			// The path box, recent folders and the dialog's start read such paths; the proof of concept's self-test checked it (#1209).
			var folder = Directory.CreateTempSubdirectory("Weevil.LocationTests.").FullName;

			try
			{
				var zipPath = Path.Combine(folder, "Bundle.zip");
				using (var memory = new MemoryStream())
				{
					using (var inner = new System.IO.Compression.ZipArchive(memory, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
					{
						using var writer = new StreamWriter(inner.CreateEntry("Old.log").Open());
						writer.Write("old log\n");
					}

					using System.IO.Compression.ZipArchive outer = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create);
					using Stream entry = outer.CreateEntry("Nested/Inner.zip").Open();
					entry.Write(memory.ToArray());
				}

				Location.Parse(Path.Combine(zipPath, "Nested", "Inner.zip", "Old.log"), IsZip)
					.Should().Be(Location.ForArchive(zipPath, "Nested/Inner.zip", "Old.log"));
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1328)]
		public void GivenTheRootOfADrive_WhenShown_ThenItKeepsItsSeparatorAndReadsBackAsTheRoot()
		{
			// The root was shown as `C:`, which Windows reads as the current folder on drive C, so a recent folder saved that way opened elsewhere.
			var root = Path.GetPathRoot(Path.GetTempPath())!;
			var drive = Location.ForFileSystem(root);

			drive.ToString().Should().Be(root);
			Location.Parse(drive.ToString(), IsZip).Should().Be(drive);
		}

		[TestMethod]
		[WorkItem(1328)]
		public void GivenADrivesLetterAlone_WhenParsed_ThenItIsTheRootOfTheDrive()
		{
			// `C:`, saved by a build before #1328 or typed in the path box, is the root, as in Explorer.
			var root = Path.GetPathRoot(Path.GetTempPath())!;

			Location.Parse(root.TrimEnd(Path.DirectorySeparatorChar), IsZip).Should().Be(Location.ForFileSystem(root));
		}

		private static bool IsZip(string name) => name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
	}
}
