namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	[TestClass]
	public class SavedParseTests
	{
		[TestMethod]
		public void GivenAParseByThisPluginAndBuild_WhenCompared_ThenItIsTheSameParser()
		{
			SavedParse parse = Create();

			parse.IsSameParser(new DefaultPlugin(), StorageMode.Ram).Should().BeTrue();
		}

		[TestMethod]
		[DataRow("PluginName")]
		[DataRow("PluginBuild")]
		[DataRow("CoreBuild")]
		[DataRow("CommonBuild")]
		[DataRow("StorageMode")]
		public void GivenAParseThatDiffersInOnePart_WhenCompared_ThenItIsNotTheSameParser(string part)
		{
			// A saved table is reused only when the same plugin, the same builds of it and of Weevil, and the same storage mode would make it (#1023).
			SavedParse p = Create();

			var changed = new SavedParse(
				part == "PluginName" ? p.PluginName + "x" : p.PluginName,
				part == "PluginBuild" ? Guid.NewGuid() : p.PluginBuildId,
				part == "CoreBuild" ? Guid.NewGuid() : p.CoreBuildId,
				part == "CommonBuild" ? Guid.NewGuid() : p.CommonBuildId,
				part == "StorageMode" ? StorageMode.Disk : p.StorageMode,
				p.StartOffset,
				p.EndOffset,
				[]);

			changed.IsSameParser(new DefaultPlugin(), StorageMode.Ram).Should().BeFalse();
		}

		[TestMethod]
		[DataRow(new ulong[] { 1, 2 }, 100_000L, true, DisplayName = "The same bytes")]
		[DataRow(new ulong[] { 1, 9, 5, 6 }, 200_000L, true, DisplayName = "A log that grew, whose last block of before is now longer")]
		[DataRow(new ulong[] { 8, 9, 5, 6 }, 200_000L, false, DisplayName = "A log that grew and changed in a block read in full before")]
		[DataRow(new ulong[] { 1, 3 }, 100_000L, false, DisplayName = "A log that changed in its last block")]
		public void GivenAnEarlierParse_WhenComparedWithALaterOne_ThenTheirBytesAreTheSameOnlyIfEveryBlockBothReadMatches(ulong[] laterChecksums, long laterEnd, bool isSame)
		{
			// A log changed in the middle has its line index rebuilt, since the line index checks only the start and end of the log (#1023).
			SavedParse p = Create();
			var earlier = new SavedParse(p.PluginName, p.PluginBuildId, p.CoreBuildId, p.CommonBuildId, StorageMode.Ram, 0, 100_000, [1, 2]);
			var later = new SavedParse(p.PluginName, p.PluginBuildId, p.CoreBuildId, p.CommonBuildId, StorageMode.Disk, 0, laterEnd, laterChecksums);

			earlier.HasSameBytesAs(later).Should().Be(isSame);
		}

		/// <summary>
		/// A parse of nothing by the default plugin, in RAM, with this build of Weevil.
		/// </summary>
		private static SavedParse Create()
		{
			var path = Path.Combine(AppContext.BaseDirectory, "Unread.log");
			return SavedParse.Create(new DefaultPlugin(), StorageMode.Ram, LogFileSource.Create(path, 0, 0, 0, []));
		}
	}
}
