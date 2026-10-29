namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Text;

	/// <summary>
	/// A range of a log file's bytes, exactly as they are in the file, shared by the records whose content it holds (#1133).
	/// </summary>
	/// <remarks>
	/// Holding the bytes instead of one string per record uses about half the memory for a UTF-8 log.
	/// A chunk never changes, so records on any thread can decode it.
	/// </remarks>
	public sealed class ContentChunk
	{
		private readonly byte[] _bytes;
		private readonly Encoding _encoding;

		/// <param name="fileOffset">Where the bytes start in the log file.</param>
		/// <param name="bytes">The bytes, which are copied, so the records' content cannot change afterwards.</param>
		/// <param name="encoding">The log's encoding, without a byte order mark, which turns invalid bytes into U+FFFD. It is copied too.</param>
		public ContentChunk(long fileOffset, ReadOnlySpan<byte> bytes, Encoding encoding)
		{
			ArgumentNullException.ThrowIfNull(encoding);

			this.FileOffset = fileOffset;
			_bytes = bytes.ToArray();
			_encoding = (Encoding)encoding.Clone();
		}

		public long FileOffset { get; }

		public int Length => _bytes.Length;

		/// <summary>
		/// Returns the text of the bytes at <paramref name="offset"/>, as <see cref="IRecord.Content"/> returns it.
		/// </summary>
		/// <param name="isMultiLine">The bytes span several lines, which are joined with <c>\r\n</c> whatever the file's own line breaks.</param>
		internal string GetString(int offset, int length, bool isMultiLine)
		{
			if (!isMultiLine)
			{
				return _encoding.GetString(_bytes, offset, length);
			}

			char[] buffer = [];
			var count = Decode(offset, length, isMultiLine, ref buffer);

			return new string(buffer, 0, count);
		}

		/// <summary>
		/// Decodes the bytes at <paramref name="offset"/> into <paramref name="buffer"/>, which grows when needed, and returns how many characters it holds.
		/// </summary>
		/// <remarks>
		/// The same text as <see cref="GetString"/>, without allocating a string.
		/// </remarks>
		internal int Decode(int offset, int length, bool isMultiLine, ref char[] buffer)
		{
			var maximum = _encoding.GetMaxCharCount(length);

			if (buffer.Length < maximum)
			{
				buffer = new char[maximum];
			}

			var count = _encoding.GetChars(_bytes, offset, length, buffer, 0);

			if (isMultiLine)
			{
				var lineBreaksToWiden = CountLoneLineBreaks(buffer.AsSpan(0, count));

				if (lineBreaksToWiden > 0)
				{
					if (buffer.Length < count + lineBreaksToWiden)
					{
						Array.Resize(ref buffer, count + lineBreaksToWiden);
					}

					WidenLoneLineBreaks(buffer, count, lineBreaksToWiden);
					count += lineBreaksToWiden;
				}
			}

			return count;
		}

		/// <summary>
		/// Counts the line breaks that are a lone <c>\r</c> or <c>\n</c>, each of which becomes <c>\r\n</c>.
		/// </summary>
		/// <remarks>
		/// A line of a log cannot hold <c>\r</c> or <c>\n</c>, because each ends a line, so every one found is a line break.
		/// </remarks>
		private static int CountLoneLineBreaks(ReadOnlySpan<char> text)
		{
			var count = 0;

			for (var i = 0; i < text.Length; i++)
			{
				if (text[i] == '\r')
				{
					if (i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}
					else
					{
						count++;
					}
				}
				else if (text[i] == '\n')
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// Turns each lone <c>\r</c> or <c>\n</c> among the first <paramref name="count"/> characters into <c>\r\n</c>,
		/// working from the end, so the text grows in place.
		/// </summary>
		private static void WidenLoneLineBreaks(char[] text, int count, int lineBreaksToWiden)
		{
			var j = count + lineBreaksToWiden - 1;

			for (var i = count - 1; i >= 0; i--)
			{
				var c = text[i];

				if (c == '\r' || c == '\n')
				{
					text[j--] = '\n';
					text[j--] = '\r';

					if (c == '\n' && i > 0 && text[i - 1] == '\r')
					{
						i--;
					}
				}
				else
				{
					text[j--] = c;
				}
			}
		}
	}
}
