namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// The options the Open As dialog shows for the log selected, and how they say to open it (#1216): Weevil's own, or a plugin's from <see cref="IOpenAsOptionsPlugin"/>.
	/// </summary>
	/// <remarks>
	/// When the options implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>, raised on the UI thread, the dialog enables Open as <see cref="CanOpen"/> changes.
	/// Otherwise it reads <see cref="CanOpen"/> only after <see cref="SelectAsync"/>.
	/// </remarks>
	public interface IOpenAsOptions
	{
		/// <summary>
		/// Whether the log can be opened with these options. Open stays disabled until it is.
		/// </summary>
		bool CanOpen { get; }

		/// <summary>
		/// RAM or Disk.
		/// </summary>
		StorageMode StorageMode { get; }

		/// <summary>
		/// The part of the log to open: <see cref="LogPart.Whole"/> for the whole log.
		/// </summary>
		LogPart Part { get; }

		/// <summary>
		/// What the plugin needs to read the log, such as a file format's version, given to <see cref="IEngineBuilder.UsingContext"/>. Empty for none.
		/// </summary>
		ContextDictionary Context { get; }

		/// <summary>
		/// Shows the options for the item the analyst selected: a log, or <see langword="null"/> when a folder, a zip or nothing is selected.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread. Read the log, or its sidecar, off it: it may be on a slow network drive.
		/// An item in a zip is not extracted until the analyst opens it.
		/// </remarks>
		Task SelectAsync(BrowserItem file);
	}
}
