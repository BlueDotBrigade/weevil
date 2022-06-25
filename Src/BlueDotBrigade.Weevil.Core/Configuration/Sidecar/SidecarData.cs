
namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System.Collections.Immutable;
	using System.Runtime.Serialization;
	using Data;
	using Filter;
	using Navigation;

	[DataContract]
	public class SidecarData
	{
		public ImmutableArray<IRecord> Records { get; set; }
		public ContextDictionary Context { get; set; }
		public IFilterTraits FilterTraits { get; set; }
		public TableOfContents TableOfContents { get; set; }
		public string UserRemarks { get; set; }
	}
}
