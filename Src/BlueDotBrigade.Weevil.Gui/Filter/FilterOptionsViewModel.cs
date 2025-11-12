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
		public bool IncludeDebugRecords { get; set; }

		public bool IncludeTraceRecords { get; set; }

		public bool IncludePinned { get; set; }

		public bool IsManualFilter { get; set; }

		public bool IsFilterCaseSensitive { get; set; }

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
