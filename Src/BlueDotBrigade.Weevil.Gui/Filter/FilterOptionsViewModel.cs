namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.ComponentModel;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using PostSharp.Patterns.Model;

	[NotifyPropertyChanged()]
	internal class FilterOptionsViewModel : INotifyPropertyChanged
	{
		#region Fields
		private FilterType _filterExpressionType;
		private bool _includeDebugRecords;
		private bool _includeTraceRecords;
		private bool _includePinned;
		private bool _isManualFilter;
		private bool _isFilterCaseSensitive;
		#endregion

		#region Object Lifetime
		public FilterOptionsViewModel()
		{
			this.IncludeDebugRecords = true;
			this.IncludeTraceRecords = true;
			this.IncludePinned = true;
			this.IsManualFilter = false;
			this.IsFilterCaseSensitive = true;
			this.FilterExpressionType = FilterType.RegularExpression;
		}
		#endregion

		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		public event System.EventHandler FilterOptionsChanged;
		#endregion

		#region Properties
		public bool IncludeDebugRecords
		{
			get => _includeDebugRecords;
			set
			{
				if (_includeDebugRecords != value)
				{
					_includeDebugRecords = value;
					OnPropertyChanged(nameof(IncludeDebugRecords));
					OnFilterOptionsChanged();
				}
			}
		}

		public bool IncludeTraceRecords
		{
			get => _includeTraceRecords;
			set
			{
				if (_includeTraceRecords != value)
				{
					_includeTraceRecords = value;
					OnPropertyChanged(nameof(IncludeTraceRecords));
					OnFilterOptionsChanged();
				}
			}
		}

		public bool IncludePinned
		{
			get => _includePinned;
			set
			{
				if (_includePinned != value)
				{
					_includePinned = value;
					OnPropertyChanged(nameof(IncludePinned));
					OnFilterOptionsChanged();
				}
			}
		}

		public bool IsManualFilter
		{
			get => _isManualFilter;
			set
			{
				if (_isManualFilter != value)
				{
					_isManualFilter = value;
					OnPropertyChanged(nameof(IsManualFilter));
					OnFilterOptionsChanged();
				}
			}
		}

		public bool IsFilterCaseSensitive
		{
			get => _isFilterCaseSensitive;
			set
			{
				if (_isFilterCaseSensitive != value)
				{
					_isFilterCaseSensitive = value;
					OnPropertyChanged(nameof(IsFilterCaseSensitive));
					OnFilterOptionsChanged();
				}
			}
		}

		public FilterType FilterExpressionType
		{
			get => _filterExpressionType;
			set
			{
				if (_filterExpressionType != value)
				{
					_filterExpressionType = value;
					OnPropertyChanged(nameof(FilterExpressionType));
					OnFilterOptionsChanged();
				}
			}
		}
		#endregion

		#region Private Methods
		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OnFilterOptionsChanged()
		{
			FilterOptionsChanged?.Invoke(this, System.EventArgs.Empty);
		}
		#endregion
	}
}
