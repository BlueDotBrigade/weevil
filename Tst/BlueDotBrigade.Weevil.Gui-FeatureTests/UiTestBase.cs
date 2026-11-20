namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	public abstract class UiTestBase 
	{
		private readonly List<FilterViewModel> _viewModels = new List<FilterViewModel>();

		internal IUiDispatcher UiDispatcher { get; private set; }

		public UiTestBase()
		{
			this.UiDispatcher = new UiDispatcherFake();
		}

		/// <summary>
		/// Closes the logs the test's view models opened, as Weevil does when it closes: each releases its lock (#1254),
		/// so the next test can open the same log, and a test can delete the folder it was in.
		/// </summary>
		/// <remarks>
		/// MSTest runs it after a derived class's own cleanup, which calls it first when it deletes a folder.
		/// </remarks>
		[TestCleanup]
		public void CloseViewModels()
		{
			foreach (FilterViewModel viewModel in _viewModels)
			{
				viewModel.Close();
			}

			_viewModels.Clear();
		}

		/// <summary>
		/// Keeps a view model the test created, so <see cref="CloseViewModels"/> closes its log.
		/// </summary>
		internal FilterViewModel Track(FilterViewModel viewModel)
		{
			_viewModels.Add(viewModel);
			return viewModel;
		}
	}
}