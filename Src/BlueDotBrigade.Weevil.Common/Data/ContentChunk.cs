namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Buffers;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// A range of a log file's bytes, exactly as they are in the file, shared by the records whose content it holds (#1133).
	/// </summary>
	/// <remarks>
	/// Holding the bytes instead of one string per record uses about half the memory for a UTF-8 log.
	/// A chunk on file holds no bytes: its records' content is read back from the log when it is read (#1025).
	/// A chunk never changes, so records on any thread can decode it.
	/// </remarks>
	public sealed class ContentChunk
	{
		// The bytes, or null for a chunk on file, which reads them from `_source`.
		private readonly byte[] _bytes;
		private readonly LogFileSource _source;
		private readonly int _length;
		private readonly Encoding _encoding;

		/// <param name="fileOffset">Where the bytes start in the log file.</param>
		/// <param name="bytes">The bytes, which are copied, so the records' content cannot change afterwards.</param>
		/// <param name="encoding">The log's encoding, without a byte order mark, which turns invalid bytes into U+FFFD. It is copied too.</param>
		public ContentChunk(long fileOffset, ReadOnlySpan<byte> bytes, Encoding encoding)
		{
			ArgumentNullException.ThrowIfNull(encoding);

			this.FileOffset = fileOffset;
			_bytes = bytes.ToArray();
			_length = _bytes.Length;
			_encoding = (Encoding)encoding.Clone();
		}

		/// <summary>
		/// Creates a chunk on file, which holds no bytes and reads them from <paramref name="source"/> when its records' content is read (#1025).
		/// </summary>
		/// <param name="source">The log, which checks that the bytes read are the bytes parsed.</param>
		/// <param name="fileOffset">Where the bytes start in the log file.</param>
		/// <param name="length">How many bytes the chunk spans.</param>
		/// <param name="encoding">The log's encoding, without a byte order mark, which turns invalid bytes into U+FFFD. It is copied.</param>
		public ContentChunk(LogFileSource source, long fileOffset, int length, Encoding encoding)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(encoding);
			ArgumentOutOfRangeException.ThrowIfNegative(length);

			if (fileOffset < source.StartOffset || fileOffset > source.EndOffset - length)
			{
				throw new ArgumentOutOfRangeException(nameof(fileOffset), fileOffset, "The chunk is expected to be within the range of the log that was parsed.");
			}

			this.FileOffset = fileOffset;
			_source = source;
			_length = length;
			_encoding = (Encoding)encoding.Clone();
		}

		public long FileOffset { get; }

		public int Length => _length;

		/// <summary>
		/// The chunk holds no bytes, and reads them back from the log.
		/// </summary>
		internal bool IsOnFile => _source != null;

		/// <summary>
		/// Reads the whole chunk from the log into <paramref name="destination"/>, which is <see cref="Length"/> bytes long.
		/// </summary>
		/// <exception cref="LogFileChangedException">The log was truncated, replaced or corrupted since it was parsed.</exception>
		internal void ReadFromFile(Span<byte> destination)
		{
			_source.Read(this.FileOffset, destination);
		}

		/// <summary>
		/// Returns the text of the bytes at <paramref name="offset"/>, as <see cref="IRecord.Content"/> returns it.
		/// </summary>
		/// <param name="isMultiLine">The bytes span several lines, which are joined with <c>\r\n</c> whatever the file's own line breaks.</param>
		internal string GetString(int offset, int length, bool isMultiLine)
		{
			if (!isMultiLine && !this.IsOnFile)
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
		/// A chunk on file reads only the blocks of the log that hold the bytes.
		/// </remarks>
		internal int Decode(int offset, int length, bool isMultiLine, ref char[] buffer)
		{
			if (!this.IsOnFile)
			{
				return Decode(_bytes.AsSpan(offset, length), isMultiLine, ref buffer);
			}

			var bytes = ArrayPool<byte>.Shared.Rent(length);

			try
			{
				_source.Read(this.FileOffset + offset, bytes.AsSpan(0, length));

				return Decode(bytes.AsSpan(0, length), isMultiLine, ref buffer);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(bytes);
			}
		}

		/// <summary>
		/// Decodes <paramref name="bytes"/>, some of this chunk's, into <paramref name="buffer"/>, as <see cref="Decode(int, int, bool, ref char[])"/> does.
		/// </summary>
		internal int Decode(ReadOnlySpan<byte> bytes, bool isMultiLine, ref char[] buffer)
		{
			var maximum = _encoding.GetMaxCharCount(bytes.Length);

			if (buffer.Length < maximum)
			{
				buffer = new char[maximum];
			}

			var count = _encoding.GetChars(bytes, buffer);

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
			var i = 0;
			while (true)
			{
				var found = text.Slice(i).IndexOfAny('\r', '\n');
				if (found < 0)
				{
					return count;
				}
				i += found;
				if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
				{
					i += 2;
				}
				else
				{
					count++;
					i++;
				}
			}
		}

		/// <summary>
		/// Turns each lone <c>\r</c> or <c>\n</c> among the first <paramref name="count"/> characters into <c>\r\n</c>,
		/// working from the end, so the text grows in place.
		/// </summary>
		private static void WidenLoneLineBreaks(char[] text, int count, int lineBreaksToWiden)
		{
			var end = count;
			var j = count + lineBreaksToWiden;
			while (lineBreaksToWiden > 0)
			{
				var k = text.AsSpan(0, end).LastIndexOfAny('\r', '\n');
				var tail = end - (k + 1);
				text.AsSpan(k + 1, tail).CopyTo(text.AsSpan(j - tail));
				j -= tail;
				if (text[k] == '\n' && k > 0 && text[k - 1] == '\r')
				{
					end = k - 1;
				}
				else
				{
					end = k;
					lineBreaksToWiden--;
				}
				text[--j] = '\n';
				text[--j] = '\r';
			}
		}
	}
}
