#nullable enable

namespace Weevil.VirtualListPoC;

using System.ComponentModel;

/// <summary>
/// One row of the grid, created only while WPF needs it: for a row on screen, a lookup or a scroll.
/// </summary>
/// <remarks>
/// <para>
/// It holds no record: content is generated on each access, as #988 will decode it from raw bytes.
/// Comments and pins are read from, and written to, the annotation store; the row keeps a copy only for its bindings.
/// </para>
/// <para>
/// Rows are equal by line number, because WPF finds items with <c>IndexOf</c> and <c>Equals</c>,
/// and a row may be created more than once for the same record.
/// </para>
/// </remarks>
internal sealed class RowViewModel : INotifyPropertyChanged, IEquatable<RowViewModel>
{
	private readonly AnnotationStore _annotations;
	private string _comment;
	private bool _isPinned;
	private bool _isSelected;

	public RowViewModel(int position, int recordNumber, TimeSpan elapsedTime, bool isSelected, AnnotationStore annotations)
	{
		this.Position = position;
		this.RecordNumber = recordNumber;
		this.LineNumber = SyntheticLog.GetLineNumber(recordNumber);
		this.ElapsedTime = elapsedTime;
		_isSelected = isSelected;
		_annotations = annotations;
		_comment = annotations.GetComment(this.LineNumber);
		_isPinned = annotations.IsPinned(this.LineNumber);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// The row's position in the filter results it was created for.
	/// </summary>
	public int Position { get; }

	public int RecordNumber { get; }

	public int LineNumber { get; }

	public SeverityType Severity => SyntheticLog.GetSeverity(this.RecordNumber);

	public bool IsMultiLine => SyntheticLog.IsMultiLine(this.RecordNumber);

	public string Content => SyntheticLog.GetContent(this.RecordNumber);

	public DateTime CreatedAt => SyntheticLog.GetTimestamp(this.RecordNumber);

	/// <summary>
	/// The time since the previous record in the same results, calculated by the view (decision 1 of the #990 plan).
	/// </summary>
	public TimeSpan ElapsedTime { get; }

	public bool IsPinned
	{
		get => _isPinned;
		set
		{
			if (_isPinned != value)
			{
				_isPinned = value;
				_annotations.SetPinned(this.LineNumber, value, this);
				OnPropertyChanged(nameof(this.IsPinned));
			}
		}
	}

	public string Comment
	{
		get => _comment;
		set
		{
			value ??= string.Empty;
			if (_comment != value)
			{
				_comment = value;
				_annotations.SetComment(this.LineNumber, value, this);
				OnPropertyChanged(nameof(this.Comment));
			}
		}
	}

	/// <summary>
	/// Whether the range selection includes this row. Only used when WPF's own selection is off.
	/// </summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged(nameof(this.IsSelected));
			}
		}
	}

	/// <summary>
	/// Reads the row's annotations again, after the store reports a change to its line.
	/// </summary>
	/// <remarks>
	/// The comment is not read back after the row's own write: the comment box sends its text 250 ms after
	/// the last key press, so an echo of an older write would overwrite text still being typed.
	/// The pin is always read back, because adding a comment pins the record.
	/// </remarks>
	public void Refresh(object? source)
	{
		var isPinned = _annotations.IsPinned(this.LineNumber);
		if (_isPinned != isPinned)
		{
			_isPinned = isPinned;
			OnPropertyChanged(nameof(this.IsPinned));
		}

		if (!ReferenceEquals(source, this))
		{
			var comment = _annotations.GetComment(this.LineNumber);
			if (_comment != comment)
			{
				_comment = comment;
				OnPropertyChanged(nameof(this.Comment));
			}
		}
	}

	public bool Equals(RowViewModel? other)
	{
		return other is not null && other.LineNumber == this.LineNumber;
	}

	public override bool Equals(object? obj)
	{
		return obj is RowViewModel other && Equals(other);
	}

	public override int GetHashCode()
	{
		return this.LineNumber;
	}

	public override string ToString()
	{
		return "Line " + this.LineNumber;
	}

	private void OnPropertyChanged(string propertyName)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
