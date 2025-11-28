namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;

	/// <summary>
	/// Plain object that holds all filter option settings.
	/// </summary>
	public class FilterOptions : INotifyPropertyChanged
	{
		private bool _includeDebugRecords;
		private bool _includeTraceRecords;
		private bool _includePinned;
		private bool _isFilterCaseSensitive;
		private FilterType _filterExpressionType;

		public FilterOptions()
		{
			this.IncludeDebugRecords = true;
			this.IncludeTraceRecords = true;
			this.IncludePinned = true;
			this.IsFilterCaseSensitive = true;
			this.FilterExpressionType = FilterType.RegularExpression;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IncludeDebugRecords
		{
			get => _includeDebugRecords;
			set
			{
				if (_includeDebugRecords != value)
				{
					_includeDebugRecords = value;
					OnPropertyChanged(nameof(IncludeDebugRecords));
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
				}
			}
		}

		/// <summary>
		/// Converts the filter options to a configuration dictionary compatible with FilterCriteria.
		/// </summary>
		public Dictionary<string, object> ToConfiguration()
		{
			var configuration = new Dictionary<string, object>();

			if (this.IncludePinned)
			{
				configuration.Add("IncludePinned", this.IncludePinned);
			}

			configuration.Add("IsCaseSensitive", this.IsFilterCaseSensitive);

			if (!this.IncludeDebugRecords)
			{
				configuration.Add("HideDebugRecords", true);
			}

			if (!this.IncludeTraceRecords)
			{
				configuration.Add("HideTraceRecords", true);
			}

			return configuration;
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
