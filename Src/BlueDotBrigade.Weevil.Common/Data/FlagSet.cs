namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
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
			ThrowIfNegative(lineNumber);

			lock (_padlock)
			{
				SetBit(lineNumber, isFlagged);
			}
		}

		/// <summary>
		/// Unflags every line, then flags <paramref name="lineNumbers"/>, in one step.
		/// </summary>
		public void FlagOnly(IEnumerable<int> lineNumbers)
		{
			lock (_padlock)
			{
				Array.Clear(_words, 0, _words.Length);

				foreach (var lineNumber in lineNumbers)
				{
					ThrowIfNegative(lineNumber);
					SetBit(lineNumber, true);
				}
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

		// Called while holding the lock.
		private void SetBit(int lineNumber, bool isFlagged)
		{
			var word = lineNumber / BitsPerWord;

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

		private static void ThrowIfNegative(int lineNumber)
		{
			if (lineNumber < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "A line number cannot be negative.");
			}
		}

		private static long Mask(int lineNumber)
		{
			return 1L << (lineNumber % BitsPerWord);
		}
	}
}
