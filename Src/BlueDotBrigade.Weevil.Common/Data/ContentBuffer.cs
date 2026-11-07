namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// Holds the content of one record at a time, so code that reads many records, such as a filter, reads each record's content once (#1131).
	/// </summary>
	/// <remarks>
	/// Not thread-safe: use one per thread. What <see cref="Read"/> returns is valid until it is called for another record.
	/// A record that holds the log's bytes is decoded into memory the buffer reuses, so reading it allocates no string (#1133).
	/// </remarks>
	public sealed class ContentBuffer
	{
		private IRecord _record;

		// The record's string, or null when its bytes were decoded into the first `_length` characters of `_chars`.
		private string _content = string.Empty;
		private char[] _chars = [];
		private int _length;

		/// <summary>
		/// Returns the content of <paramref name="record"/>, and reads it only if this buffer does not hold it already.
		/// </summary>
		public ReadOnlySpan<char> Read(IRecord record)
		{
			ArgumentNullException.ThrowIfNull(record);

			if (!ReferenceEquals(record, _record))
			{
				_record = record;
				_content = record is Record stored && stored.TryDecodeContent(ref _chars, out _length)
					? null
					: record.Content;
			}

			return _content is null ? _chars.AsSpan(0, _length) : _content;
		}

		/// <summary>
		/// Returns the text of the <paramref name="length"/> bytes at <paramref name="offset"/> in <paramref name="chunk"/>,
		/// as <see cref="Read(IRecord)"/> returns it for a record that holds them, so a list over a record table can read content without creating records (#1150).
		/// </summary>
		/// <param name="isMultiLine">The bytes span several lines, which are joined with <c>\r\n</c> whatever the file's own line breaks.</param>
		public ReadOnlySpan<char> Read(ContentChunk chunk, int offset, int length, bool isMultiLine)
		{
			ArgumentNullException.ThrowIfNull(chunk);

			if (offset < 0 || length < 0 || offset > chunk.Length - length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The content is expected to be within the chunk.");
			}

			// The buffer no longer holds a record's content, so the next Read(IRecord) reads its record again.
			_record = null;
			_content = null;
			_length = chunk.Decode(offset, length, isMultiLine, ref _chars);

			return _chars.AsSpan(0, _length);
		}
	}
}
