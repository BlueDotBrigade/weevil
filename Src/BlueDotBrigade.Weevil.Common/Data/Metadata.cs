namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;

	public class Metadata : INotifyPropertyChanged
	{
		/// <summary>
		/// Indicates the system was unable to determine the amount of time between records.
		/// </summary>
		public static readonly TimeSpan ElapsedTimeUnknown = TimeSpan.MinValue;

		private TimeSpan _elapsedTime;

		// The comment, pin and flag live in stores kept by line number (#1013).
		// Until Core attaches this record to a view of its log, they are kept in stores of its own.
		private ViewAnnotations _view;
		private int _lineNumber;
		private bool _isAttached;

		public event PropertyChangedEventHandler PropertyChanged;

		public Metadata()
		{
			_elapsedTime = ElapsedTimeUnknown;
		}

		/// <summary>
		/// Makes this record's comment and pin read and write the log's annotations, and its flag the view's flags.
		/// Values set before are copied to them. Called by Core when a log is opened.
		/// </summary>
		public void Attach(ViewAnnotations view, int lineNumber)
		{
			if (view is null)
			{
				throw new ArgumentNullException(nameof(view));
			}

			if (!_isAttached)
			{
				if (this.Comment.Length > 0)
				{
					view.Annotations.SetComment(lineNumber, this.Comment);
				}

				// After the comment, which may have pinned the record.
				if (this.Comment.Length > 0 || this.IsPinned)
				{
					view.Annotations.SetPinned(lineNumber, this.IsPinned);
				}

				if (this.IsFlagged)
				{
					view.Flags.SetFlagged(lineNumber, true);
				}
			}

			_view = view;
			_lineNumber = lineNumber;
			_isAttached = true;
		}

		/// <summary>
		/// Returns <see langword="true"/> if the metadata has been assigned a valid elapsed time value.
		/// </summary>
		public static bool ValidateElapsedTime(Metadata metadata)
		{
			if (metadata is null)
			{
				throw new ArgumentNullException(nameof(metadata));
			}

			return metadata.ElapsedTime >= TimeSpan.Zero;
		}

		public bool HasElapsedTime => Metadata.ValidateElapsedTime(this);

		/// <summary>
		/// Represents a user's comment.
		/// </summary>
		public string Comment
		{
			get => _view?.Annotations.GetComment(_lineNumber) ?? string.Empty;

			set
			{
				var previous = this.Comment;

				if (previous != value)
				{
					// The store pins the record when a comment is added, and unpins it when the comment is removed.
					GetView().Annotations.SetComment(_lineNumber, value);

					if (string.IsNullOrWhiteSpace(value) != string.IsNullOrWhiteSpace(previous))
					{
						RaisePropertyChanged(nameof(this.IsPinned));
					}

					RaisePropertyChanged();
					RaisePropertyChanged(nameof(this.HasComment));
				}
			}
		}

		/// <summary>
		/// When <see langword="True"/>, indicates the record has a user comment assigned to it.
		/// </summary>
		public bool HasComment => !string.IsNullOrWhiteSpace(this.Comment);

		public void UpdateUserComment(string comment)
		{
			this.Comment = this.HasComment ? $"{this.Comment}, {comment}" : comment;
		}

		/// <summary>
		/// Represents the amount of time that has elapsed between the current the preceeding record.
		/// </summary>
		public TimeSpan ElapsedTime
		{
			get => _elapsedTime;
			set
			{
				if (_elapsedTime != value)
				{
					_elapsedTime = value;
					RaisePropertyChanged();
				}
			}
		}

		/// <summary>
		/// Indicates that the record should remain visible, regardless of the filter that is being applied.
		/// </summary>
		public bool IsPinned
		{
			get => _view?.Annotations.IsPinned(_lineNumber) ?? false;
			set
			{
				if (this.IsPinned != value)
				{
					GetView().Annotations.SetPinned(_lineNumber, value);
					RaisePropertyChanged();
				}
			}
		}

		public bool IsFlagged
		{
			get => _view?.Flags.IsFlagged(_lineNumber) ?? false;
			set
			{
				if (this.IsFlagged != value)
				{
					GetView().Flags.SetFlagged(_lineNumber, value);
					RaisePropertyChanged();
				}
			}
		}

		// A record that is not attached keeps its values in stores of its own, created when a value is first set.
		private ViewAnnotations GetView()
		{
			return _view ??= new ViewAnnotations();
		}

		private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;

			if (handler != null)
			{
				handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public override string ToString()
		{
			return $@"Elapsedtime={this.ElapsedTime}, IsPinned={this.IsPinned}, IsFlagged={this.IsFlagged}, Comment={this.Comment}";
		}
	}
}