namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;

	/// <summary>
	/// The comments and pins of a log, kept by line number and shared by every view of it (#1013).
	/// </summary>
	/// <remarks>
	/// Writes are serialized by one lock. Reads never wait, so filters can read from many threads.
	/// </remarks>
	public sealed class AnnotationStore
	{
		// One byte per line.
		private const byte PinnedBit = 0b01;
		private const byte CommentBit = 0b10;

		private readonly object _padlock = new object();
		private readonly ConcurrentDictionary<int, string> _comments = new ConcurrentDictionary<int, string>();
		private byte[] _lines;

		public AnnotationStore()
			: this(0)
		{
			// nothing to do
		}

		/// <param name="lineCount">The number of lines expected. The store grows when a later line is annotated.</param>
		public AnnotationStore(int lineCount)
		{
			_lines = new byte[System.Math.Max(lineCount, 0) + 1];
		}

		public string GetComment(int lineNumber)
		{
			return _comments.TryGetValue(lineNumber, out var comment) ? comment : string.Empty;
		}

		/// <summary>
		/// The line has a comment that is not only white space.
		/// </summary>
		public bool HasComment(int lineNumber)
		{
			return (GetBits(lineNumber) & CommentBit) != 0;
		}

		public bool IsPinned(int lineNumber)
		{
			return (GetBits(lineNumber) & PinnedBit) != 0;
		}

		/// <summary>
		/// Adding a comment pins the record, and removing it unpins the record.
		/// </summary>
		public void SetComment(int lineNumber, string comment)
		{
			comment ??= string.Empty;

			lock (_padlock)
			{
				var previous = GetComment(lineNumber);

				if (previous == comment)
				{
					return;
				}

				var bits = GetBits(lineNumber);

				if (string.IsNullOrWhiteSpace(comment) && !string.IsNullOrWhiteSpace(previous))
				{
					bits &= unchecked((byte)~PinnedBit);
				}
				else if (!string.IsNullOrWhiteSpace(comment) && string.IsNullOrWhiteSpace(previous))
				{
					bits |= PinnedBit;
				}

				SetCommentText(lineNumber, comment, ref bits);
				SetBits(lineNumber, bits);
			}
		}

		public void SetPinned(int lineNumber, bool isPinned)
		{
			lock (_padlock)
			{
				var bits = GetBits(lineNumber);
				SetBits(lineNumber, isPinned ? (byte)(bits | PinnedBit) : (byte)(bits & ~PinnedBit));
			}
		}

		/// <summary>
		/// Restores a saved comment and pin exactly as saved, without the comment rules.
		/// </summary>
		public void Load(int lineNumber, string comment, bool isPinned)
		{
			lock (_padlock)
			{
				var bits = isPinned ? PinnedBit : (byte)0;
				SetCommentText(lineNumber, comment ?? string.Empty, ref bits);
				SetBits(lineNumber, bits);
			}
		}

		private void SetCommentText(int lineNumber, string comment, ref byte bits)
		{
			if (comment.Length == 0)
			{
				_comments.TryRemove(lineNumber, out _);
			}
			else
			{
				_comments[lineNumber] = comment;
			}

			bits = string.IsNullOrWhiteSpace(comment)
				? (byte)(bits & ~CommentBit)
				: (byte)(bits | CommentBit);
		}

		private byte GetBits(int lineNumber)
		{
			var lines = Volatile.Read(ref _lines);
			return lineNumber >= 0 && lineNumber < lines.Length ? lines[lineNumber] : (byte)0;
		}

		// Called while holding the lock.
		private void SetBits(int lineNumber, byte bits)
		{
			if (lineNumber < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "A line number cannot be negative.");
			}

			if (lineNumber >= _lines.Length)
			{
				var grown = new byte[System.Math.Max(lineNumber + 1, _lines.Length * 2)];
				Array.Copy(_lines, grown, _lines.Length);
				Volatile.Write(ref _lines, grown);
			}

			_lines[lineNumber] = bits;
		}
	}
}
