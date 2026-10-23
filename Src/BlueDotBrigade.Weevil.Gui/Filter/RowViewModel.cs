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
		private readonly IAnnotations _annotations;

		// The comment the grid last read or wrote, so a change to the pin alone leaves the comment box alone.
		private string _shownComment = string.Empty;
		private bool _isSelected;

		public RowViewModel(IRecord record, IAnnotations annotations)
		{
			this.Record = record ?? throw new ArgumentNullException(nameof(record));
			_annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public IRecord Record { get; }

		public int LineNumber => this.Record.LineNumber;

		public SeverityType Severity => this.Record.Severity;

		/// <remarks>
		/// The view calculates elapsed times after each filter, and each filter creates new rows.
		/// </remarks>
		public TimeSpan ElapsedTime => this.Record.Metadata?.ElapsedTime ?? Metadata.ElapsedTimeUnknown;

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
