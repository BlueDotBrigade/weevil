namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;

	[DebuggerDisplay("Level={Level}, Name={Name}")]
	public class Section : ISection
	{
		public Section()
		{
			this.LineNumber = 0;
			this.Level = 0;
			this.Name = string.Empty;
		}

		public Section(string name, int level, long byteOffset, int lineNumber)
		{
			this.Name = name ?? throw new ArgumentOutOfRangeException(nameof(name), "A string was expected.");
			this.Level = level > 0
				? level
				: throw new ArgumentOutOfRangeException(nameof(level),
					$"A value greater than zero was expected. Value={level}");
			this.ByteOffset = byteOffset >= 0
				? byteOffset
				: throw new ArgumentOutOfRangeException(nameof(byteOffset),
					$"A value greater than or equal to zero was expected. Value={byteOffset}");
			this.LineNumber = lineNumber > 0
				? lineNumber
				: throw new ArgumentOutOfRangeException(nameof(lineNumber),
					$"A value greater than or equal to 1 was expected. Value={lineNumber}");
		}

		public string Name { get; set; }

		public int Level { get; set; }

		public int LineNumber { get; set; }

		/// <summary>
		/// Represents the starting position of the section within the source file.
		/// </summary>
		/// <remarks>
		/// The <see cref="ByteOffset"/> has intentionally been hidden from the public API.
		/// </remarks>
		public long ByteOffset { get; set; }
	}
}