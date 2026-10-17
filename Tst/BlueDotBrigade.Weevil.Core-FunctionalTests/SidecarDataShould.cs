namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SidecarDataShould
	{
		[TestMethod]
		public void UseLineNumberWhenLoadingUserComment()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("UseLineNumberWhenLoadingUserComment.log"))
				.Open();

			engine.Selector.Select(31);

			engine.Clear(ClearOperation.BeforeSelected);

			engine.Filter.Results[1].LineNumber.Should().Be(32);
			engine.Filter.Results[1].Metadata.Comment.Should().Be("Index31");
		}

		[TestMethod]
		public void KeepCommentsForRecordsNoLongerInMemory()
		{
			IEngine firstEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			firstEngine[0].Metadata.Comment.Should().Be("First");

			firstEngine.Selector.Select(5);
			firstEngine.Clear(ClearOperation.BeforeSelected);
			firstEngine.Save(true);
			firstEngine = null;

			IEngine secondEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			secondEngine[0].Metadata.Comment.Should().Be("First");
		}

		// Issue #977: saving the sidecar writes byte offsets taken from the line index.
		[TestMethod]
		public void GivenCommentAndBookmark_WhenSaving_ThenSidecarHasTheirByteOffsets()
		{
			// Not under the temporary folder, where files are treated as extracted from a compressed archive and nothing is saved.
			var directory = Path.Combine(AppContext.BaseDirectory, nameof(SidecarDataShould), Guid.NewGuid().ToString("N"));
			var logFilePath = Path.Combine(directory, "Application.log");
			Directory.CreateDirectory(directory);

			try
			{
				// Every line is 9 bytes, so line N begins at byte (N - 1) * 9.
				File.WriteAllText(logFilePath, string.Concat(Enumerable.Range(1, 300).Select(lineNumber => $"Line {lineNumber:000}\n")));

				IEngine engine = Engine.UsingPath(logFilePath).Open();
				engine[149].Metadata.Comment = "A comment on line 150";
				engine.Bookmarks.Create(1, "A bookmark on line 180", 180);
				engine.Save(true);

				XElement data = XDocument.Load($"{logFilePath}.xml").Root.Elements().First(e => e.Name.LocalName == "CommonData");
				XElement Child(XElement parent, string name) => parent.Elements().First(e => e.Name.LocalName == name);

				Child(Child(Child(Child(data, "Records"), "Record"), "RelatesTo"), "ByteOffset").Value.Should().Be("1341");
				Child(Child(Child(Child(data, "Bookmarks"), "Bookmark"), "Record"), "ByteOffset").Value.Should().Be("1611");
				File.Exists($"{logFilePath}.idx").Should().BeTrue();
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}
	}
}
