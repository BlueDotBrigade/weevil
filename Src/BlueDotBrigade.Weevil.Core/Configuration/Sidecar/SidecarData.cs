
namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System;
	using System.Collections.Immutable;
	using Data;
	using Filter;
	using Navigation;

	public class SidecarData
	{
		public ImmutableArray<IRecord> Records { get; set; }

		/// <summary>
		/// The comments and pins, by line number.
		/// </summary>
		public IAnnotations Annotations { get; set; }
		public ContextDictionary Context { get; set; }
		public IFilterTraits FilterTraits { get; set; }
		public TableOfContents TableOfContents { get; set; }
		public string SourceFileRemarks { get; set; }

		public ImmutableArray<Region> Regions{ get; set; }

		public ImmutableArray<Bookmark> Bookmarks { get; set; }

		/// <summary>
		/// Returns the byte offset where a line begins, or -1 when it is not known.
		/// </summary>
		public Func<int, long> ByteOffsetOfLine { get; set; }
	}
}
