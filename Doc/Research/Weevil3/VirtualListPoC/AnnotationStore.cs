#nullable enable

namespace Weevil.VirtualListPoC;

internal sealed class AnnotationChangedEventArgs : EventArgs
{
	/// <summary>
	/// The line number of a bulk change, such as Unpin All, which raises one event for every line.
	/// </summary>
	public const int AllLines = -1;

	public AnnotationChangedEventArgs(int lineNumber, object? source)
	{
		this.LineNumber = lineNumber;
		this.Source = source;
	}

	public int LineNumber { get; }

	/// <summary>
	/// The object that made the change, so a row can ignore its own writes.
	/// </summary>
	public object? Source { get; }
}

/// <summary>
/// Comments and pins kept by line number, standing in for the session's annotation store (#1013).
/// </summary>
/// <remarks>
/// It keeps both comment rules: adding a comment pins the record, and removing it unpins it.
/// One lock guards every write, and <see cref="Changed"/> is raised outside the lock, on the writer's thread.
/// </remarks>
internal sealed class AnnotationStore
{
	private readonly object _padlock = new object();
	private readonly SortedDictionary<int, string> _comments = new SortedDictionary<int, string>();
	private readonly SortedSet<int> _pins = new SortedSet<int>();

	public event EventHandler<AnnotationChangedEventArgs>? Changed;

	public int CommentCount
	{
		get
		{
			lock (_padlock)
			{
				return _comments.Count;
			}
		}
	}

	public int PinCount
	{
		get
		{
			lock (_padlock)
			{
				return _pins.Count;
			}
		}
	}

	public string GetComment(int lineNumber)
	{
		lock (_padlock)
		{
			return _comments.TryGetValue(lineNumber, out var comment) ? comment : string.Empty;
		}
	}

	public bool IsPinned(int lineNumber)
	{
		lock (_padlock)
		{
			return _pins.Contains(lineNumber);
		}
	}

	public void SetComment(int lineNumber, string? comment, object? source)
	{
		lock (_padlock)
		{
			if (string.IsNullOrEmpty(comment))
			{
				_comments.Remove(lineNumber);
				_pins.Remove(lineNumber);
			}
			else
			{
				_comments[lineNumber] = comment;
				_pins.Add(lineNumber);
			}
		}

		this.Changed?.Invoke(this, new AnnotationChangedEventArgs(lineNumber, source));
	}

	public void SetPinned(int lineNumber, bool isPinned, object? source)
	{
		lock (_padlock)
		{
			if (isPinned)
			{
				_pins.Add(lineNumber);
			}
			else
			{
				_pins.Remove(lineNumber);
			}
		}

		this.Changed?.Invoke(this, new AnnotationChangedEventArgs(lineNumber, source));
	}

	public int UnpinAll()
	{
		int count;
		lock (_padlock)
		{
			count = _pins.Count;
			_pins.Clear();
		}

		this.Changed?.Invoke(this, new AnnotationChangedEventArgs(AnnotationChangedEventArgs.AllLines, this));
		return count;
	}

	public int RemoveComments()
	{
		int count;
		lock (_padlock)
		{
			count = _comments.Count;
			foreach (var lineNumber in _comments.Keys)
			{
				_pins.Remove(lineNumber);
			}

			_comments.Clear();
		}

		this.Changed?.Invoke(this, new AnnotationChangedEventArgs(AnnotationChangedEventArgs.AllLines, this));
		return count;
	}

	/// <summary>
	/// Returns the commented lines after <paramref name="lineNumber"/>, then the ones before it, as navigation wraps.
	/// </summary>
	public int[] GetCommentedLinesFrom(int lineNumber)
	{
		lock (_padlock)
		{
			return _comments.Keys.Where(l => l > lineNumber).Concat(_comments.Keys.Where(l => l <= lineNumber)).ToArray();
		}
	}

	public int[] GetPinnedLinesFrom(int lineNumber)
	{
		lock (_padlock)
		{
			return _pins.GetViewBetween(lineNumber + 1, int.MaxValue).Concat(_pins.GetViewBetween(int.MinValue, lineNumber)).ToArray();
		}
	}
}
