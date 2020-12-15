namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using Microsoft.Practices.Prism.Commands;
	using Microsoft.Practices.Prism.Mvvm;

	public class SelectFileViewModel : BindableBase
	{
		private DelegateCommand okCommand;
		private string selectedFilename;
		private DelegateCommand cancelCommand;

		public SelectFileViewModel(string[] filenames)
		{
			this.Filenames = filenames;
		}

		public string[] Filenames { get; }

		public string SelectedFilename
		{
			get => selectedFilename;
			set
			{
				selectedFilename = value;
				this.OkCommand.RaiseCanExecuteChanged();
			}
		}

		public DelegateCommand OkCommand => okCommand ?? (okCommand = new DelegateCommand(Ok, CanOk));
		public DelegateCommand CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand(Cancel));

		private void Cancel()
		{
			this.SelectedFilename = null;
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		private bool CanOk()
		{
			return this.SelectedFilename != null;
		}

		private void Ok()
		{
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler CloseRequested;
	}
}
