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
