namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Navigation;

	public interface IPersist
	{
		void Load(
			ImmutableArray<IRecord> allRecords,
			out ContextDictionary context,
			out string sourceFileRemarks,
			out List<string> inclusiveFilterHistory,
			out List<string> exclusiveFilterHistory,
			out List<Section> tableOfContents,
			out List<Region> regions, 
			out List<Bookmark>bookmarks);

		void Save(SidecarData data, bool deleteBackup);

	}
}