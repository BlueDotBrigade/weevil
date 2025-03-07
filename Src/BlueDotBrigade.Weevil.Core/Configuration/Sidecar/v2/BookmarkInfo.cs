namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Name={Name}, Record={Record.LineNumber}")]
	[DataContract(Name = "Bookmark")]
	public class BookmarkInfo
	{
		public BookmarkInfo()
		{
			this.Name = string.Empty;
			this.Record = new RelatesTo();
		}

		[DataMember(Order = 100)]
		public string Name { get; set; }

		[DataMember(Order = 200)]
		public RelatesTo Record { get; set; }
	}
}