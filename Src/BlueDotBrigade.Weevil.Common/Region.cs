namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Diagnostics;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.DataContracts;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("Name={this.Name}, MinLineNumber={this.Minimum.LineNumber}, MaxLineNumber={this.Maximum.LineNumber}")]
	public class Region
	{
		[DataMember]
		public string Name { get; }

		[DataMember]
		public RelatesTo Minimum { get; }

		[DataMember]
		public RelatesTo Maximum { get; }

		public Region(int startLineNumber, int endLineNumber)
			: this(string.Empty, startLineNumber, endLineNumber)
		{
			// nothing to do
		}

		public Region(string name, int startLineNumber, int endLineNumber)
		{
			this.Name = name;
			this.Minimum = new RelatesTo()
			{
				LineNumber = startLineNumber,
				ByteOffset = -1
			};
			this.Maximum = new RelatesTo()
			{
				LineNumber = endLineNumber,
				ByteOffset = -1
			};
		}

		public Region(string name, RelatesTo startsAt, RelatesTo endsAt)
		{
			this.Name = name ?? string.Empty;
			this.Minimum = startsAt ?? throw new ArgumentNullException(nameof(startsAt));
			this.Maximum = endsAt ?? throw new ArgumentNullException(nameof(endsAt));
		}

		public bool OverlapsWith(Region other)
		{
			return this.Minimum.LineNumber <= other.Maximum.LineNumber && this.Maximum.LineNumber >= other.Minimum.LineNumber;
		}

		public bool Contains(int lineNumber)
		{
			return lineNumber >= this.Minimum.LineNumber && lineNumber <= this.Maximum.LineNumber;
		}
	}
}