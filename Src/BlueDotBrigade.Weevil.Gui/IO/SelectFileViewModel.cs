namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Prism.Commands;
	using Prism.Mvvm;

	public class SelectFileViewModel : BindableBase, INotifyPropertyChanged
	{
		private DelegateCommand okCommand;
		private string selectedFilename;
		private DelegateCommand cancelCommand;
		private string[] _filenames;

		public SelectFileViewModel(string[] filenames)
		{
			this.Filenames = filenames ?? throw new ArgumentNullException(nameof(filenames));
		}

		public string[] Filenames
		{
			get
			{
				return _filenames;
			}
			set
			{
				_filenames = value.Where(x => !x.ToUpper().EndsWith(".LOG.XML")).ToArray();
				RaisePropertyChanged(nameof(Filenames));
			}
		}

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

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

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
