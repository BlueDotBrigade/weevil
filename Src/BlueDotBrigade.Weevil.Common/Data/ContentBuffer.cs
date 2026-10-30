namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// Holds the content of one record at a time, so code that reads many records, such as a filter, reads each record's content once (#1131).
	/// </summary>
	/// <remarks>
	/// Not thread-safe: use one per thread. What <see cref="Read"/> returns is valid until it is called for another record.
	/// </remarks>
	public sealed class ContentBuffer
	{
		private IRecord _record;
		private string _content = string.Empty;

		/// <summary>
		/// Returns the content of <paramref name="record"/>, and reads it only if this buffer does not hold it already.
		/// </summary>
		public ReadOnlySpan<char> Read(IRecord record)
		{
			if (!ReferenceEquals(record, _record))
			{
				_content = record.Content;
				_record = record;
			}

			return _content;
		}
	}
}
