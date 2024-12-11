namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Name={Name}, MinLineNumber={Minimum.LineNumber}, MaxLineNumber={Maximum.LineNumber}")]
	[DataContract(Name = "Bookend")]
	public class BookendInfo
	{
		public BookendInfo()
		{
			this.Name = string.Empty;
			this.Minimum = new RelatesTo();
			this.Maximum = new RelatesTo();
		}

		[DataMember(Order = 100)]
		public string Name { get; set; }

		[DataMember(Order = 200)]
		public RelatesTo Minimum { get; set; }

		[DataMember(Order = 300)]
		public RelatesTo Maximum { get; set; }
	}
}