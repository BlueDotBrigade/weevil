namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents sidecar data that is common to all plugins.
	/// </summary>
	public class CommonData
	{
		/// <summary>
		/// Represents sidecar data that is common to all plugins.
		/// </summary>
		public CommonData()
		{
			this.Context = new ContextDictionary();
			this.UserRemarks = string.Empty;
			this.TableOfContents = new List<SectionInfo>();
			this.Regions = new List<RegionInfo>();
			this.FilterHistory = new FilterHistory();
			this.Records = new List<RecordInfo>();
		}

		[DataMember]
		public ContextDictionary Context { get; set; }

		[DataMember]
		public string UserRemarks { get; set; }

		[DataMember]
		public List<SectionInfo> TableOfContents { get; set; }

		[DataMember]
		public FilterHistory FilterHistory { get; set; }

		[DataMember]
		public List<RecordInfo> Records { get; set; }

		[DataMember]
		public List<RegionInfo> Regions { get; set; }
	}
}