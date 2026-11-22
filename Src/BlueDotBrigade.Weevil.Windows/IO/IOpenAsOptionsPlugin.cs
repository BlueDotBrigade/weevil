namespace BlueDotBrigade.Weevil.Windows.IO
{
	/// <summary>
	/// Optional for an <see cref="IPlugin"/>: it shows its own options in Weevil's Open As dialog, in place of Weevil's, while one of its logs is selected (#1216).
	/// </summary>
	/// <remarks>
	/// A plugin without it uses Weevil's options: the part of the log to open, and RAM or Disk.
	/// Weevil finds the selected log's plugin through <see cref="IPlugin.CheckCompatibility"/>, except for a log in a zip or one stored online only, such as on OneDrive,
	/// which use Weevil's options: reading them to check would extract or download them.
	/// </remarks>
	public interface IOpenAsOptionsPlugin
	{
		/// <summary>
		/// Creates this plugin's options for one Open As dialog. Weevil keeps them while the dialog is open, so they can remember a choice from one log to the next.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread.
		/// </remarks>
		IOpenAsOptions CreateOpenAsOptions();

		/// <summary>
		/// Creates what the dialog shows for <paramref name="options"/>: typically a WPF user control whose data context is <paramref name="options"/>.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread, once for each <see cref="IOpenAsOptions"/>. The dialog has no theme of its own: Weevil's styles apply to standard controls.
		/// </remarks>
		/// <param name="options">The options created by <see cref="CreateOpenAsOptions"/>.</param>
		object CreateOpenAsView(IOpenAsOptions options);
	}
}
