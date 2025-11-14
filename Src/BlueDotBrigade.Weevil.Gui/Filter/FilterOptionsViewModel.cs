namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// View model for filter options that exposes the FilterOptions model.
	/// </summary>
	internal class FilterOptionsViewModel : INotifyPropertyChanged
	{
		public FilterOptionsViewModel()
		{
			this.Options = new FilterOptions();
			this.Options.PropertyChanged += OnOptionsPropertyChanged;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler OptionsChanged;

		/// <summary>
		/// Gets the filter options model containing all settings.
		/// </summary>
		public FilterOptions Options { get; }

		private void OnOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// Bubble up property changes
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Options.{e.PropertyName}"));
			
			// Notify that options have changed
			OptionsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
