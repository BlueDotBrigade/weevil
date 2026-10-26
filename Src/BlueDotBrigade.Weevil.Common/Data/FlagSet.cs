namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Threading;

	/// <summary>
	/// The records an analyzer flagged, one bit per line. Each view of a log has its own (#1013).
	/// </summary>
	/// <remarks>
	/// Writes are serialized by one lock. Reads never wait, so filters can read from many threads.
	/// </remarks>
	public sealed class FlagSet : IFlags
	{
		private const int BitsPerWord = 64;

		private readonly object _padlock = new object();
		private long[] _words;

		public FlagSet()
			: this(0)
		{
			// nothing to do
		}

		/// <param name="lineCount">The number of lines expected. The set grows when a later line is flagged.</param>
		public FlagSet(int lineCount)
		{
			_words = new long[(System.Math.Max(lineCount, 0) / BitsPerWord) + 1];
		}

		public bool IsFlagged(int lineNumber)
		{
			var words = Volatile.Read(ref _words);
			var word = lineNumber / BitsPerWord;

			return lineNumber >= 0 && word < words.Length && (words[word] & Mask(lineNumber)) != 0;
		}

		public void SetFlagged(int lineNumber, bool isFlagged)
		{
			if (lineNumber < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "A line number cannot be negative.");
			}

			var word = lineNumber / BitsPerWord;

			lock (_padlock)
			{
				if (word >= _words.Length)
				{
					if (!isFlagged)
					{
						return;
					}

					var grown = new long[System.Math.Max(word + 1, _words.Length * 2)];
					Array.Copy(_words, grown, _words.Length);
					Volatile.Write(ref _words, grown);
				}

				_words[word] = isFlagged
					? _words[word] | Mask(lineNumber)
					: _words[word] & ~Mask(lineNumber);
			}
		}

		/// <summary>
		/// Unflags every line, including lines whose records are no longer in memory.
		/// </summary>
		public void Clear()
		{
			lock (_padlock)
			{
				Array.Clear(_words, 0, _words.Length);
			}
		}

		private static long Mask(int lineNumber)
		{
			return 1L << (lineNumber % BitsPerWord);
		}
	}
}
