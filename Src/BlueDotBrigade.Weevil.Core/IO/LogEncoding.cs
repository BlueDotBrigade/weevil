namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Buffers.Binary;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;

	/// <summary>
	/// A log's encoding, as found from its byte order mark: the size of each character's code unit, its byte order, and the length of the mark.
	/// </summary>
	/// <remarks>
	/// Both of Weevil's line scanners, <see cref="LineIndex"/> and <see cref="LogLineReader"/>, find line breaks through it,
	/// so they count lines alike, as <see cref="StreamReader.ReadLine"/> does: <c>\n</c>, <c>\r</c> and <c>\r\n</c> end a line.
	/// </remarks>
	/// <param name="UnitSize">The size of each code unit in bytes: 1 for UTF-8, 2 for UTF-16, 4 for UTF-32.</param>
	/// <param name="IsBigEndian">The code units are big-endian.</param>
	/// <param name="PreambleLength">The length of the byte order mark, which only the top of the file has.</param>
	internal readonly record struct LogEncoding(int UnitSize, bool IsBigEndian, int PreambleLength)
	{
		/// <summary>
		/// The code units are in this computer's byte order, so a line break is found without swapping bytes.
		/// </summary>
		private bool IsSameByteOrder => this.IsBigEndian != BitConverter.IsLittleEndian;

		/// <summary>
		/// The encoding is UTF-8, or UTF-16 in this computer's byte order, which .NET decodes many characters at a time.
		/// </summary>
		/// <remarks>
		/// UTF-16 in the other byte order and UTF-32 are decoded one character at a time, 10 to 50 times more slowly,
		/// so in RAM mode their records keep a string, which also uses less memory than UTF-32's bytes (#1134 review).
		/// In disk mode they are read back from the log like any other: they only decode more slowly (#1025, decision 6).
		/// </remarks>
		public bool IsQuickToDecode => this.UnitSize == 1 || (this.UnitSize == 2 && this.IsSameByteOrder);

		/// <summary>
		/// Detects the encoding from a byte order mark, with the same checks, in the same order, as <see cref="StreamReader"/>: UTF-8 when there is none.
		/// </summary>
		/// <remarks>
		/// Reads the top of the log, and leaves the stream's position after what it read.
		/// </remarks>
		public static LogEncoding Detect(Stream log)
		{
			Span<byte> b = stackalloc byte[4];

			log.Position = 0;
			var count = log.ReadAtLeast(b, b.Length, throwOnEndOfStream: false);

			if (count >= 2 && b[0] == 0xFE && b[1] == 0xFF)
			{
				return new LogEncoding(2, true, 2);
			}

			if (count >= 2 && b[0] == 0xFF && b[1] == 0xFE)
			{
				return count >= 4 && b[2] == 0 && b[3] == 0
					? new LogEncoding(4, false, 4)
					: new LogEncoding(2, false, 2);
			}

			if (count >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
			{
				return new LogEncoding(1, false, 3);
			}

			if (count >= 4 && b[0] == 0 && b[1] == 0 && b[2] == 0xFE && b[3] == 0xFF)
			{
				return new LogEncoding(4, true, 4);
			}

			return new LogEncoding(1, false, 0);
		}

		/// <summary>
		/// Creates the .NET encoding that decodes the log's lines, without a byte order mark.
		/// </summary>
		public Encoding ToEncoding()
		{
			return this.UnitSize switch
			{
				1 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				2 => new UnicodeEncoding(this.IsBigEndian, byteOrderMark: false),
				_ => new UTF32Encoding(this.IsBigEndian, byteOrderMark: false),
			};
		}

		/// <summary>
		/// Finds the first code unit that is <c>\r</c> or <c>\n</c>.
		/// </summary>
		/// <param name="bytes">Whole code units.</param>
		/// <param name="isCarriageReturn">The code unit found is <c>\r</c>, which a <c>\n</c> may follow.</param>
		/// <returns>The offset of the code unit in bytes, or -1 when there is none.</returns>
		public int IndexOfLineBreak(ReadOnlySpan<byte> bytes, out bool isCarriageReturn)
		{
			int index;

			switch (this.UnitSize)
			{
				case 1:
					index = bytes.IndexOfAny((byte)'\r', (byte)'\n');
					isCarriageReturn = index >= 0 && bytes[index] == (byte)'\r';
					return index;

				case 2:
					ReadOnlySpan<ushort> units16 = MemoryMarshal.Cast<byte, ushort>(bytes);
					var carriageReturn16 = this.IsSameByteOrder ? (ushort)'\r' : BinaryPrimitives.ReverseEndianness((ushort)'\r');
					var lineFeed16 = this.IsSameByteOrder ? (ushort)'\n' : BinaryPrimitives.ReverseEndianness((ushort)'\n');
					index = units16.IndexOfAny(carriageReturn16, lineFeed16);
					isCarriageReturn = index >= 0 && units16[index] == carriageReturn16;
					return index < 0 ? -1 : index * 2;

				default:
					ReadOnlySpan<uint> units32 = MemoryMarshal.Cast<byte, uint>(bytes);
					var carriageReturn32 = this.IsSameByteOrder ? '\r' : BinaryPrimitives.ReverseEndianness((uint)'\r');
					var lineFeed32 = this.IsSameByteOrder ? '\n' : BinaryPrimitives.ReverseEndianness((uint)'\n');
					index = units32.IndexOfAny(carriageReturn32, lineFeed32);
					isCarriageReturn = index >= 0 && units32[index] == carriageReturn32;
					return index < 0 ? -1 : index * 4;
			}
		}

		/// <summary>
		/// The code unit is <c>\n</c>, which completes a <c>\r\n</c> line break after a <c>\r</c>.
		/// </summary>
		/// <param name="unit">One code unit.</param>
		public bool IsLineFeed(ReadOnlySpan<byte> unit)
		{
			return this.UnitSize switch
			{
				1 => unit[0] == (byte)'\n',
				2 => (this.IsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(unit) : BinaryPrimitives.ReadUInt16LittleEndian(unit)) == '\n',
				_ => (this.IsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(unit) : BinaryPrimitives.ReadUInt32LittleEndian(unit)) == '\n',
			};
		}
	}
}
