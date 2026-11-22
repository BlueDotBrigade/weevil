namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// The options the Open As dialog shows for the log selected, and how they say to open it (#1216): Weevil's own, or a plugin's from <see cref="IOpenAsOptionsProvider"/>.
	/// </summary>
	/// <remarks>
	/// When the options implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>, raised on the UI thread, the dialog enables Open as <see cref="CanOpen"/> changes.
	/// Otherwise it reads <see cref="CanOpen"/> only after <see cref="SelectAsync"/>.
	///
	/// When a plugin's options implement <see cref="System.IDisposable"/>, Weevil disposes them once the dialog closes, after reading how to open the log.
	/// Release a log the options read before then: an engine keeps its log from being opened again until it is disposed (#1254).
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
		/// Shows the options for the log the analyst selected. Weevil's own options are also given <see langword="null"/> when a folder, a zip or nothing is selected.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread. Update <see cref="CanOpen"/> for the new log before the first <see langword="await"/>, since Open is enabled as soon as this method yields.
		/// Read the log, or its sidecar, off the UI thread: it may be on a slow network drive. An item in a zip is not extracted until the analyst opens it.
		/// It may be called again before an earlier call ends, when the analyst selects another log, so ignore what an earlier call finds.
		/// Options are not told when other options replace them.
		/// </remarks>
		Task SelectAsync(BrowserItem file);
	}
}
