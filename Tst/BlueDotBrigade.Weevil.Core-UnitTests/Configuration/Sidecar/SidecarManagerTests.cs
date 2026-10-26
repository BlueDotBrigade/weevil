namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SidecarManagerTests
	{
		[TestMethod]
		public void Save_WhenInTemporaryDirectory_SkipsSaveOperation()
		{
			// Arrange - Use temp directory (from compressed archive scenario)
			var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.log");
			var sidecarManager = new SidecarManager(tempPath);
			var sidecarData = new SidecarData
			{
				Records = ImmutableArray<IRecord>.Empty,
				Context = new ContextDictionary(),
				FilterTraits = null,
				SourceFileRemarks = string.Empty,
				TableOfContents = null,
				Regions = ImmutableArray<Region>.Empty,
				Bookmarks = ImmutableArray<Bookmark>.Empty
			};

			// Act - should skip save and not throw exception
			sidecarManager.Save(sidecarData, false);

			// Assert - test passes if no exception is thrown and no file is created
			(System.IO.File.Exists($"{tempPath}.xml")).Should().BeFalse("Sidecar should not be created in temp directory");
		}

		[TestMethod]
		public void Save_WhenNotInTemporaryDirectory_SavesMetadata()
		{
			// Arrange - Use a non-temp directory
			var currentDir = Directory.GetCurrentDirectory();
			var tempFile = Path.Combine(currentDir, $"test_{Guid.NewGuid()}.log");
			System.IO.File.WriteAllText(tempFile, "test content");

			try
			{
				var sidecarManager = new SidecarManager(tempFile);
				var sidecarData = new SidecarData
				{
					Records = ImmutableArray<IRecord>.Empty,
					Context = new ContextDictionary(),
					FilterTraits = null,
					SourceFileRemarks = "Test remarks",
					TableOfContents = null,
					Regions = ImmutableArray<Region>.Empty,
					Bookmarks = ImmutableArray<Bookmark>.Empty
				};

				// Act
				sidecarManager.Save(sidecarData, true);

				// Assert
				var sidecarPath = $"{tempFile}.xml";
				(System.IO.File.Exists(sidecarPath)).Should().BeTrue("Sidecar file should be created in non-temp directory");
			}
			finally
			{
				// Cleanup
				try
				{
					System.IO.File.Delete(tempFile);
					System.IO.File.Delete($"{tempFile}.xml");
					System.IO.File.Delete($"{tempFile}.xml~");
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		/// <summary>
		/// Saves a sidecar with one item of each kind, and returns the saved byte offset of each, by the item's name.
		/// </summary>
		private static Dictionary<string, long> SaveAndReadByteOffsets(Func<int, long> byteOffsetOfLine)
		{
			var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"test_{Guid.NewGuid()}.log");
			var sidecarFilePath = $"{logFilePath}.xml";

			IRecord record = R.WithContent(5, "Record with a comment");
			var annotations = new AnnotationStore();
			annotations.SetComment(record.LineNumber, "A comment");

			try
			{
				new SidecarManager(logFilePath).Save(
					new SidecarData
					{
						Records = ImmutableArray.Create(record),
						Annotations = annotations,
						Context = new ContextDictionary(),
						SourceFileRemarks = string.Empty,
						TableOfContents = new TableOfContents(new List<Section> { new Section("Section", 1, byteOffset: 999, lineNumber: 7) }),
						Regions = ImmutableArray.Create(new Region("Region", 2, 4)),
						Bookmarks = ImmutableArray.Create(new Bookmark(1, "Bookmark", 6)),
						ByteOffsetOfLine = byteOffsetOfLine,
					},
					deleteBackup: true);

				XElement data = XDocument.Load(sidecarFilePath).Root.Elements().First(e => e.Name.LocalName == "CommonData");
				long ByteOffset(XElement relatesTo) => long.Parse(relatesTo.Elements().First(e => e.Name.LocalName == "ByteOffset").Value);
				XElement Child(XElement parent, string name) => parent.Elements().First(e => e.Name.LocalName == name);

				return new Dictionary<string, long>
				{
					{ "Record", ByteOffset(Child(Child(Child(data, "Records"), "Record"), "RelatesTo")) },
					{ "Section", ByteOffset(Child(Child(Child(data, "TableOfContents"), "Section"), "RelatesTo")) },
					{ "RegionStart", ByteOffset(Child(Child(Child(data, "Regions"), "Region"), "Minimum")) },
					{ "RegionEnd", ByteOffset(Child(Child(Child(data, "Regions"), "Region"), "Maximum")) },
					{ "Bookmark", ByteOffset(Child(Child(Child(data, "Bookmarks"), "Bookmark"), "Record")) },
				};
			}
			finally
			{
				System.IO.File.Delete(sidecarFilePath);
			}
		}

		// Issue #977: byte offsets in the sidecar come from the line index.
		[TestMethod]
		public void GivenByteOffsetOfEachLine_WhenSaving_ThenSidecarUsesThoseOffsets()
		{
			Dictionary<string, long> byteOffsets = SaveAndReadByteOffsets(lineNumber => (lineNumber - 1) * 10L);

			byteOffsets["Record"].Should().Be(40);
			byteOffsets["Section"].Should().Be(60);
			byteOffsets["RegionStart"].Should().Be(10);
			byteOffsets["RegionEnd"].Should().Be(30);
			byteOffsets["Bookmark"].Should().Be(50);
		}

		[TestMethod]
		public void GivenByteOffsetsUnknown_WhenSaving_ThenSavedOffsetsAreKept()
		{
			Dictionary<string, long> byteOffsets = SaveAndReadByteOffsets(_ => -1);

			byteOffsets["Record"].Should().Be(-1);
			byteOffsets["Section"].Should().Be(999);
			byteOffsets["Bookmark"].Should().Be(-1);
		}
	}
}
