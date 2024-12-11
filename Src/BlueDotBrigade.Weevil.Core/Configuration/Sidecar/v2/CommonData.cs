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
			this.Bookends = new List<BookendInfo>();
			this.FilterHistory = new FilterHistory();
			this.Records = new List<RecordInfo>();
		}

		[DataMember(Order = 100)]
		public ContextDictionary Context { get; set; }

		[DataMember(Order = 200)]
		public string UserRemarks { get; set; }

		[DataMember(Order = 300)]
		public List<SectionInfo> TableOfContents { get; set; }

		[DataMember(Order = 400)]
		public FilterHistory FilterHistory { get; set; }

		[DataMember(Order = 500)]
		public List<RecordInfo> Records { get; set; }

		[DataMember(Order = 600)]
		public List<BookendInfo> Bookends { get; set; }
	}
}