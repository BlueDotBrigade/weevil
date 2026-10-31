namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// What a parser found about a record, in one byte: the severity in the low bits, then one bit per flag (#1138).
	/// </summary>
	/// <remarks>
	/// Defined once for <see cref="Record"/> and the record table, and later the saved index (#1023).
	/// </remarks>
	public readonly struct RecordFacts
	{
		private const byte SeverityMask = 0b0000_0111;
		private const byte MultiLineFlag = 0b0000_1000;
		private const byte UserInterfaceThreadFlag = 0b0001_0000;
		private const byte KeepsStringFlag = 0b0010_0000;

		/// <param name="isMultiLine">The record spans several lines of the log.</param>
		/// <param name="wasGeneratedByUi">The record was written by the application's user interface thread.</param>
		/// <param name="keepsString">The record keeps its content as a string, rather than as the log's bytes.</param>
		public RecordFacts(SeverityType severity, bool isMultiLine, bool wasGeneratedByUi, bool keepsString = false)
		{
			if ((uint)severity > SeverityMask)
			{
				throw new ArgumentOutOfRangeException(nameof(severity), severity, "The severity is not a known value.");
			}

			this.Value = (byte)((byte)severity
				| (isMultiLine ? MultiLineFlag : 0)
				| (wasGeneratedByUi ? UserInterfaceThreadFlag : 0)
				| (keepsString ? KeepsStringFlag : 0));
		}

		public byte Value { get; }

		public SeverityType Severity => (SeverityType)(this.Value & SeverityMask);

		public bool IsMultiLine => (this.Value & MultiLineFlag) != 0;

		public bool WasGeneratedByUi => (this.Value & UserInterfaceThreadFlag) != 0;

		public bool KeepsString => (this.Value & KeepsStringFlag) != 0;
	}
}
