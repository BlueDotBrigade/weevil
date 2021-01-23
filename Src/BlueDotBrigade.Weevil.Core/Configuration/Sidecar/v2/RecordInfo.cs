namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Diagnostics;
	using System.Runtime.Serialization;

	[DebuggerDisplay("LineNumber={RelatesTo.LineNumber}, IsPinned={IsPinned}, Comment={Comment}")]
	[DataContract(Name = "Record")]
	public class RecordInfo
	{
		public RecordInfo()
		{
			this.RelatesTo = new RelatesTo();
		}

		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public RelatesTo RelatesTo { get; set; }

		[DataMember]
		public string Comment { get; set; }

		[DataMember]
		public bool IsPinned { get; set; }
	}
}