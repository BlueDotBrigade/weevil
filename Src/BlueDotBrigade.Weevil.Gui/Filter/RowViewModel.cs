namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.ComponentModel;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// One row of the results grid, created only when WPF asks for it (#1015).
	/// </summary>
	/// <remarks>
	/// The pin and the comment live in the annotation store: the row reads them from it and writes them to it.
	/// It raises <see cref="PropertyChanged"/> by hand, because the values change in the store, which Metalama cannot see.
	/// Rows are equal by line number: WPF finds a row with <c>IndexOf</c> and <c>Equals</c>, and a row may be created more than once for the same record.
	/// </remarks>
	internal sealed class RowViewModel : INotifyPropertyChanged, IEquatable<RowViewModel>
	{
		private static readonly char[] LineBreaks = { '\r', '\n' };

		/// <summary>
		/// Shown instead of a record's text when its log changed on disk since it was opened (#1028).
		/// </summary>
		internal const string ChangedLogPlaceholder = "\u26A0 Log changed: press Ctrl+F5";

		/// <summary>
		/// Shown instead of a record's text when its log on disk can no longer be read, for example because it was deleted (#1028).
		/// </summary>
		internal const string UnreadableLogPlaceholder = "\u26A0 Log can't be read";

		private readonly IAnnotations _annotations;
		private readonly Action<Exception> _reportFailedRead;

		// The comment the grid last read or wrote, so a change to the pin alone leaves the comment box alone.
		private string _shownComment = string.Empty;
		private bool _isSelected;

		/// <param name="reportFailedRead">Told when the record's text cannot be read from its log on disk; it may be called on any thread.</param>
		public RowViewModel(IRecord record, IAnnotations annotations, Action<Exception> reportFailedRead = null)
		{
			this.Record = record ?? throw new ArgumentNullException(nameof(record));
			_annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
			_reportFailedRead = reportFailedRead;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public IRecord Record { get; }

		public int LineNumber => this.Record.LineNumber;

		public SeverityType Severity => this.Record.Severity;

		/// <remarks>
		/// The view calculates elapsed times after each filter, and each filter creates new rows, so the value never changes.
		/// </remarks>
		public TimeSpan ElapsedTime { get; init; } = ElapsedTimes.Unknown;

		/// <summary>
		/// The row is selected in Core. WPF never selects a row (#1064).
		/// </summary>
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					RaisePropertyChanged(nameof(this.IsSelected));
				}
			}
		}

		public bool IsPinned
		{
			get => _annotations.IsPinned(this.LineNumber);
			set => _annotations.SetPinned(this.LineNumber, value, this);
		}

		public string Comment
		{
			get => _shownComment = _annotations.GetComment(this.LineNumber);
			set
			{
				// Adding a comment pins the record, and removing it unpins the record.
				_annotations.SetComment(this.LineNumber, value, this);
				_shownComment = _annotations.GetComment(this.LineNumber);

				// The box sends its text after a pause, so the pin follows what it holds, not just whether it held text before:
				// a Ctrl+P pressed while typing could otherwise toggle off a pin that the first words had set (#1066).
				if (!string.IsNullOrWhiteSpace(_shownComment))
				{
					_annotations.SetPinned(this.LineNumber, true, this);
				}

				RaisePropertyChanged(nameof(this.IsPinned));
			}
		}

		/// <summary>
		/// Returns the record's text, or a placeholder when its log on disk changed or can no longer be read (#1028).
		/// </summary>
		/// <remarks>
		/// The grid's converters read the text, and WPF does not catch a converter's exception, so Weevil would end.
		/// The row reports the failure instead, and the view decides what to do about it, once per log.
		/// In RAM mode the read cannot fail.
		/// </remarks>
		public string ReadContent()
		{
			try
			{
				return this.Record.Content;
			}
			catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
			{
				_reportFailedRead?.Invoke(exception);

				return exception is BlueDotBrigade.Weevil.IO.LogFileChangedException
					? ChangedLogPlaceholder
					: UnreadableLogPlaceholder;
			}
		}

		/// <summary>
		/// Returns the text of the item a grid binding passes to a converter: through <see cref="ReadContent()"/> for a row, or the record's own text.
		/// </summary>
		public static string ReadContent(object item)
		{
			return item is RowViewModel row
				? row.ReadContent()
				: (item as IRecord)?.Content;
		}

		/// <summary>
		/// Returns the record that a grid binding passes to a converter: the row's record, or the record itself.
		/// </summary>
		public static IRecord ToRecord(object item)
		{
			return item is RowViewModel row ? row.Record : item as IRecord;
		}

		/// <summary>
		/// Shows a change made to the row's line in the annotation store.
		/// </summary>
		/// <param name="source">The object that made the change.</param>
		/// <remarks>
		/// The comment box sends its text 250 ms after the last key, so reading the comment back could overwrite text
		/// that is still being typed. A row therefore ignores its own writes, and reads the comment back only when it changed.
		/// </remarks>
		public void Refresh(object source)
		{
			if (!ReferenceEquals(source, this))
			{
				RaisePropertyChanged(nameof(this.IsPinned));

				if (_annotations.GetComment(this.LineNumber) != _shownComment)
				{
					RaisePropertyChanged(nameof(this.Comment));
				}
			}
		}

		/// <summary>
		/// The row's name for UI Automation clients, such as screen readers: its line number and the first line of its record.
		/// </summary>
		public override string ToString()
		{
			var content = ReadContent() ?? string.Empty;
			var endOfLine = content.IndexOfAny(LineBreaks);

			return $"Line {this.LineNumber}: {(endOfLine < 0 ? content : content[..endOfLine])}";
		}

		public bool Equals(RowViewModel other)
		{
			return other is not null && other.LineNumber == this.LineNumber;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as RowViewModel);
		}

		public override int GetHashCode()
		{
			return this.LineNumber;
		}

		private void RaisePropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
