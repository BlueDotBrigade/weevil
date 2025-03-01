namespace BlueDotBrigade.Weevil.Data;

using System.Runtime.Serialization;

[DataContract(Name = "RelatesTo",
	Namespace = "http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2")]
public class RelatesTo
{
	public RelatesTo()
	{
		this.ByteOffset = -1;
		this.LineNumber = -1;
	}

	[DataMember]
	public long ByteOffset { get; set; }

	[DataMember]
	public int LineNumber { get; set; }
}