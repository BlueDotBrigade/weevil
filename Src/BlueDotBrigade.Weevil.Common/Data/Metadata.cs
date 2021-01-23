﻿namespace BlueDotBrigade.Weevil.Data
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

		private string _comment;
		private TimeSpan _elapsedTime;
		private bool _isPinned;
		private bool _isFlagged;
		private bool _wasGeneratedByUi;
		private bool _isMultiLine;

		public event PropertyChangedEventHandler PropertyChanged;

		public Metadata()
		{
			_comment = string.Empty;
			_elapsedTime = TimeSpan.Zero;
			_isPinned = false;
			_isFlagged = false;
			_wasGeneratedByUi = false;
			_isMultiLine = false;
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
			get => _comment;

			set
			{
				if (_comment != value)
				{
					if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(_comment))
					{
						this.IsPinned = false;
						RaisePropertyChanged(nameof(this.IsPinned));
					}
					else
					{
						if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_comment))
						{
							this.IsPinned = true;
							RaisePropertyChanged(nameof(this.IsPinned));
						}
					}

					_comment = value ?? string.Empty;
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
			get => _isPinned;
			set
			{
				if (_isPinned != value)
				{
					_isPinned = value;
					RaisePropertyChanged();
				}
			}
		}

		public bool IsFlagged
		{
			get => _isFlagged;
			set
			{
				if (_isFlagged != value)
				{
					_isFlagged = value;
					RaisePropertyChanged();
				}
			}
		}

		/// <summary>
		/// Indicates that the record was generated by the application's UI thread.
		/// </summary>
		public bool WasGeneratedByUi
		{
			get => _wasGeneratedByUi;
			set
			{
				if (_wasGeneratedByUi != value)
				{
					_wasGeneratedByUi = value;
					RaisePropertyChanged();
				}
			}
		}

		/// <summary>
		/// Indicates that the record includes a newline (<c>\n</c>) and/or and carriage return (<c>\r</c>) character.
		/// </summary>
		public bool IsMultiLine
		{
			get => _isMultiLine;
			set
			{
				if (_isMultiLine != value)
				{
					_isMultiLine = value;
					RaisePropertyChanged();
				}
			}
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
			return $@"Elapsedtime={this.ElapsedTime}, IsPinned={this.IsPinned}, IsFlagged={this.IsFlagged}, WasGeneratedByUi={this.WasGeneratedByUi}, IsMultiLine={this.IsMultiLine}, Comment={this.Comment}";
		}
	}
}