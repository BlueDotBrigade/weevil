namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;

	/// <summary>
	/// The header section of a saved index: the line index's own values, and which sections follow the checkpoints (#1023).
	/// </summary>
	/// <remarks>
	/// <see cref="Write"/> and <see cref="Read"/> are the only code that knows the order of its fields. See <see cref="IndexFile"/> for the file's layout.
	/// </remarks>
	internal readonly struct IndexHeader
	{
		/// <summary>
		/// The length of a SHA-256 hash, which the line index takes of the start and end of the content it scanned.
		/// </summary>
		public const int HashLength = 32;

		public int CheckpointInterval { get; init; }

		public LogEncoding Encoding { get; init; }

		public int LineCount { get; init; }

		public long IndexedLength { get; init; }

		/// <summary>
		/// Where the scan stopped, so scanning the content added to the log later carries on from there.
		/// </summary>
		public LineIndex.ScanState EndState { get; init; }

		public byte[] HeadHash { get; init; }

		public byte[] TailHash { get; init; }

		public int CheckpointCount { get; init; }

		/// <summary>
		/// The parse section follows the checkpoints.
		/// </summary>
		public bool HasParse { get; init; }

		/// <summary>
		/// The record table's sections follow the parse.
		/// </summary>
		public bool HasTable { get; init; }

		/// <summary>
		/// The values agree with each other: a table only with a parse, a checkpoint for each interval of lines, and whole hashes.
		/// </summary>
		/// <remarks>
		/// The checksum detects damage, so this guards against a file made to claim what it does not hold, such as billions of lines.
		/// </remarks>
		public bool IsConsistent =>
			(this.HasParse || !this.HasTable) &&
			this.CheckpointInterval >= 1 &&
			this.LineCount >= 0 &&
			this.CheckpointCount == (this.LineCount + (long)this.CheckpointInterval - 1) / this.CheckpointInterval &&
			this.HeadHash?.Length == HashLength &&
			this.TailHash?.Length == HashLength &&
			this.IndexedLength >= this.Encoding.PreambleLength;

		public void Write(BinaryWriter writer)
		{
			writer.Write(this.CheckpointInterval);
			writer.Write((byte)this.Encoding.UnitSize);
			writer.Write(this.Encoding.IsBigEndian);
			writer.Write((byte)this.Encoding.PreambleLength);
			writer.Write(this.LineCount);
			writer.Write(this.IndexedLength);
			writer.Write(this.EndState.IsLineStartPending);
			writer.Write(this.EndState.IsCarriageReturnPending);
			writer.Write(this.HeadHash);
			writer.Write(this.TailHash);
			writer.Write(this.CheckpointCount);
			writer.Write(this.HasParse);
			writer.Write(this.HasTable);
		}

		/// <exception cref="EndOfStreamException">The section ends before the header does.</exception>
		public static IndexHeader Read(BinaryReader reader)
		{
			return new IndexHeader
			{
				CheckpointInterval = reader.ReadInt32(),
				Encoding = new LogEncoding(UnitSize: reader.ReadByte(), IsBigEndian: reader.ReadBoolean(), PreambleLength: reader.ReadByte()),
				LineCount = reader.ReadInt32(),
				IndexedLength = reader.ReadInt64(),
				EndState = new LineIndex.ScanState
				{
					IsLineStartPending = reader.ReadBoolean(),
					IsCarriageReturnPending = reader.ReadBoolean(),
				},
				HeadHash = reader.ReadBytes(HashLength),
				TailHash = reader.ReadBytes(HashLength),
				CheckpointCount = reader.ReadInt32(),
				HasParse = reader.ReadBoolean(),
				HasTable = reader.ReadBoolean(),
			};
		}
	}
}
