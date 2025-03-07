namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.DataContracts;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Name={this.Name}, LineNumber={this.Record.LineNumber}")]
	public class Bookmark
	{
		[DataMember]
		public string Name { get; }

		[DataMember]
		public RelatesTo Record { get; }

		public Bookmark(int lineNumber)
			: this(string.Empty, lineNumber)
		{
			// nothing to do
		}

		public Bookmark(string name, int lineNumber)
		{
			this.Name = name;
			this.Record = new RelatesTo()
			{
				LineNumber = lineNumber,
				ByteOffset = -1
			};
		}
	}
}