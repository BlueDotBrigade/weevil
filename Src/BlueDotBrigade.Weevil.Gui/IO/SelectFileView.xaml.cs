namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System.Windows;

	public partial class SelectFileView : Window
	{
		public SelectFileView(SelectFileViewModel viewModel)
		{
			viewModel.CloseRequested += (sender,
				args) => Close();
			this.DataContext = viewModel;
			InitializeComponent();
		}
	}
}
