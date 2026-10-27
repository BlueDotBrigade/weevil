namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading;

	/// <summary>
	/// The comments and pins of a log, kept by line number and shared by every view of it (#1013).
	/// </summary>
	/// <remarks>
	/// Writes are serialized by one lock. Reads never wait, so filters can read from many threads.
	/// <see cref="Changed"/> is raised after the lock is released.
	/// </remarks>
	public sealed class AnnotationStore : IAnnotations
	{
		// One byte per line.
		private const byte PinnedBit = 0b01;
		private const byte CommentBit = 0b10;

		private readonly object _padlock;
		// One lock and no capacity up front: writes are already serialized by _padlock, and most lines have no comment.
		private readonly ConcurrentDictionary<int, string> _comments = new ConcurrentDictionary<int, string>(concurrencyLevel: 1, capacity: 0);
		private byte[] _lines;

		public AnnotationStore()
			: this(0)
		{
			// nothing to do
		}

		/// <param name="lineCount">The number of lines expected. The store grows when a later line is annotated.</param>
		public AnnotationStore(int lineCount)
			: this(lineCount, new object())
		{
			// nothing to do
		}

		/// <param name="lineCount">The number of lines expected. The store grows when a later line is annotated.</param>
		/// <param name="writeLock">The lock that serializes writes. A session shares one lock among all its stores (#1107).</param>
		public AnnotationStore(int lineCount, object writeLock)
		{
			_padlock = writeLock ?? throw new ArgumentNullException(nameof(writeLock));
			_lines = new byte[System.Math.Max(lineCount, 0) + 1];
		}

		public event EventHandler<AnnotationChangedEventArgs> Changed;

		public string GetComment(int lineNumber)
		{
			return _comments.TryGetValue(lineNumber, out var comment) ? comment : string.Empty;
		}

		/// <inheritdoc />
		public bool HasComment(int lineNumber)
		{
			return (GetBits(lineNumber) & CommentBit) != 0;
		}

		public bool IsPinned(int lineNumber)
		{
			return (GetBits(lineNumber) & PinnedBit) != 0;
		}

		/// <inheritdoc />
		public void SetComment(int lineNumber, string comment, object source = null)
		{
			bool hasChanged;

			lock (_padlock)
			{
				hasChanged = ReplaceComment(lineNumber, comment ?? string.Empty);
			}

			if (hasChanged)
			{
				this.Changed?.Invoke(this, new AnnotationChangedEventArgs(lineNumber, source));
			}
		}

		/// <inheritdoc />
		public void AppendComment(int lineNumber, string comment, object source = null)
		{
			comment ??= string.Empty;
			bool hasChanged;

			lock (_padlock)
			{
				var combined = HasComment(lineNumber) ? $"{GetComment(lineNumber)}, {comment}" : comment;
				hasChanged = ReplaceComment(lineNumber, combined);
			}

			if (hasChanged)
			{
				this.Changed?.Invoke(this, new AnnotationChangedEventArgs(lineNumber, source));
			}
		}

		/// <inheritdoc />
		public void SetComment(IEnumerable<int> lineNumbers, string comment, object source = null)
		{
			comment ??= string.Empty;
			var hasChanged = false;

			try
			{
				lock (_padlock)
				{
					foreach (var lineNumber in lineNumbers)
					{
						hasChanged |= ReplaceComment(lineNumber, comment);
					}
				}
			}
			finally
			{
				// Also when a line is rejected, so the lines already changed are reported.
				RaiseChangedForManyLines(hasChanged, source);
			}
		}

		// Called while holding the lock. Adding a comment pins the record, and removing it unpins the record.
		private bool ReplaceComment(int lineNumber, string comment)
		{
			var previous = GetComment(lineNumber);

			if (previous == comment)
			{
				return false;
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

			return true;
		}

		/// <inheritdoc />
		public void SetPinned(int lineNumber, bool isPinned, object source = null)
		{
			ThrowIfNegative(lineNumber);

			lock (_padlock)
			{
				var bits = GetBits(lineNumber);
				var changed = isPinned ? (byte)(bits | PinnedBit) : (byte)(bits & ~PinnedBit);

				if (changed == bits)
				{
					return;
				}

				SetBits(lineNumber, changed);
			}

			this.Changed?.Invoke(this, new AnnotationChangedEventArgs(lineNumber, source));
		}

		/// <inheritdoc />
		public void SetPinned(IEnumerable<int> lineNumbers, bool isPinned, object source = null)
		{
			var hasChanged = false;

			try
			{
				lock (_padlock)
				{
					foreach (var lineNumber in lineNumbers)
					{
						ThrowIfNegative(lineNumber);

						var bits = GetBits(lineNumber);
						var changed = isPinned ? (byte)(bits | PinnedBit) : (byte)(bits & ~PinnedBit);

						if (changed != bits)
						{
							SetBits(lineNumber, changed);
							hasChanged = true;
						}
					}
				}
			}
			finally
			{
				// Also when a line is rejected, so the lines already changed are reported.
				RaiseChangedForManyLines(hasChanged, source);
			}
		}

		/// <inheritdoc />
		public void RemoveComments(IEnumerable<int> lineNumbers, object source = null)
		{
			var hasChanged = false;

			try
			{
				lock (_padlock)
				{
					foreach (var lineNumber in lineNumbers)
					{
						var previous = GetComment(lineNumber);

						if (previous.Length > 0)
						{
							var bits = GetBits(lineNumber);

							if (!string.IsNullOrWhiteSpace(previous))
							{
								bits &= unchecked((byte)~PinnedBit);
							}

							SetCommentText(lineNumber, string.Empty, ref bits);
							SetBits(lineNumber, bits);
							hasChanged = true;
						}
					}
				}
			}
			finally
			{
				RaiseChangedForManyLines(hasChanged, source);
			}
		}

		private void RaiseChangedForManyLines(bool hasChanged, object source)
		{
			if (hasChanged)
			{
				this.Changed?.Invoke(this, new AnnotationChangedEventArgs(AnnotationChangedEventArgs.ManyLines, source));
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

		// Called while holding the lock, before SetBits.
		private void SetCommentText(int lineNumber, string comment, ref byte bits)
		{
			// Checked before the text changes, so a rejected line is left without a comment.
			ThrowIfNegative(lineNumber);

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
			ThrowIfNegative(lineNumber);

			if (lineNumber >= _lines.Length)
			{
				var grown = new byte[System.Math.Max(lineNumber + 1, _lines.Length * 2)];
				Array.Copy(_lines, grown, _lines.Length);
				Volatile.Write(ref _lines, grown);
			}

			_lines[lineNumber] = bits;
		}

		private static void ThrowIfNegative(int lineNumber)
		{
			if (lineNumber < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "A line number cannot be negative.");
			}
		}
	}
}
