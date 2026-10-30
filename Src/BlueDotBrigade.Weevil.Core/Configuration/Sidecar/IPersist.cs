namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Navigation;

	public interface IPersist
	{
		/// <param name="annotations">Receives every saved comment and pin, or <see langword="null"/> to leave them out.</param>
		void Load(
			RecordList allRecords,
			AnnotationStore annotations,
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