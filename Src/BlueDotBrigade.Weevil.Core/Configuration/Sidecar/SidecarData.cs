
namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System.Collections.Immutable;
	using Data;
	using Filter;
	using Navigation;

	public class SidecarData
	{
		public ImmutableArray<IRecord> Records { get; set; }
		public ContextDictionary Context { get; set; }
		public IFilterTraits FilterTraits { get; set; }
		public TableOfContents TableOfContents { get; set; }
		public string SourceFileRemarks { get; set; }

		public ImmutableArray<Region> Regions{ get; set; }
	}
}
