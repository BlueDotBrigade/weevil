namespace BlueDotBrigade.Weevil.Windows.IO
{
	/// <summary>
	/// Supplies a plugin's own options for Weevil's Open As dialog, shown in place of Weevil's while one of the plugin's logs is selected (#1216).
	/// </summary>
	/// <remarks>
	/// Optional: a plugin without one uses Weevil's options, the part of the log to open and RAM or Disk.
	///
	/// Export it with <c>[Export(typeof(IOpenAsOptionsProvider))]</c> from an assembly of its own in the <c>Wpf</c> folder inside Weevil's <c>Plugins</c> folder.
	/// Only Weevil's window loads that folder, so the plugin's <see cref="IPlugin"/> class, which Core, the CLI and PowerShell load, needs no WPF.
	///
	/// Weevil finds the log's plugin as it does to open the log: the first whose <see cref="IPlugin.CheckCompatibility"/> accepts it.
	/// For a log in a zip, the plugin checks only the log's start, read from the zip into a temporary file of the same name, unless an earlier open kept a copy:
	/// the log is not extracted. A log stored online only, such as on OneDrive, uses Weevil's options: reading any of it would download it.
	/// Options read a zipped log's sidecar straight from the zip, for example with <c>ContainerReader.OpenRead(ContainerReader.GetSibling(file.Location, file.Name + ".xml"))</c>.
	///
	/// Weevil creates the provider off the UI thread, before it loads the plugins in <c>Plugins</c>, and disposes the container that created it.
	/// So its constructor must create no WPF object and use nothing from the plugin's own assembly, and it must not implement <see cref="System.IDisposable"/>:
	/// do that work in <see cref="CreateOpenAsOptions"/> and <see cref="CreateOpenAsView"/>. Put any other assembly it needs beside it in <c>Plugins\Wpf</c>.
	/// </remarks>
	public interface IOpenAsOptionsProvider
	{
		/// <summary>
		/// The <see cref="IPlugin.Name"/> of the plugin whose logs these options are for.
		/// </summary>
		string PluginName { get; }

		/// <summary>
		/// Creates the options for one Open As dialog. Weevil keeps them while the dialog is open, so they can remember a choice from one log to the next.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread, the first time one of the plugin's logs is selected.
		/// </remarks>
		IOpenAsOptions CreateOpenAsOptions();

		/// <summary>
		/// Creates what the dialog shows for <paramref name="options"/>: typically a WPF user control whose data context is <paramref name="options"/>.
		/// </summary>
		/// <remarks>
		/// Called on the UI thread, once for the options. The dialog has no theme of its own: Weevil's styles apply to standard controls.
		/// </remarks>
		/// <param name="options">The options created by <see cref="CreateOpenAsOptions"/>.</param>
		object CreateOpenAsView(IOpenAsOptions options);
	}
}
