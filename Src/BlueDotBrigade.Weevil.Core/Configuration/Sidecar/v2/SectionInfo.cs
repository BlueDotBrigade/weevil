namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Level={Level}, Name={Name}")]
	[DataContract(Name = "Section")]
	public class SectionInfo
	{
		public SectionInfo()
		{
			this.Name = string.Empty;
			this.Level = -1;
			this.RelatesTo = new RelatesTo();
		}

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public int Level { get; set; }

		[DataMember]
		public RelatesTo RelatesTo { get; set; }
	}
}