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
	}
}
