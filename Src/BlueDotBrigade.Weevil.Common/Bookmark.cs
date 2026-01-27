namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.DataContracts;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Id={this.Id}, Name={this.Name}, LineNumber={this.Record.LineNumber}")]
	public class Bookmark
	{
		[DataMember]
		public int Id { get; }

		[DataMember]
		public string Name { get; }

		[DataMember]
		public RelatesTo Record { get; }

		public Bookmark(int lineNumber)
			: this(0, string.Empty, lineNumber)
		{
			// nothing to do
		}

		public Bookmark(string name, int lineNumber)
			: this(0, name, lineNumber)
		{
			// nothing to do
		}

		public Bookmark(int id, string name, int lineNumber)
		{
			this.Id = id;
			this.Name = name;
			this.Record = new RelatesTo()
			{
				LineNumber = lineNumber,
				ByteOffset = -1
			};
		}
	}
}