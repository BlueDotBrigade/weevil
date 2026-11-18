namespace BlueDotBrigade.Weevil.Gui.IO
{
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// The log chosen in the Open As dialog, and how to open it (#1215).
	/// </summary>
	/// <param name="LogPath">The log on disk: where it is, or where it was extracted to from a zip, with its sidecar and index.</param>
	/// <param name="ExtractedFolder">The folder a log in a zip was extracted to, for Weevil to delete when the log closes: <see langword="null"/> for a log on disk.</param>
	/// <param name="StorageMode">RAM or Disk.</param>
	/// <param name="Advice">What Weevil suggested for the log, for the log Weevil writes.</param>
	internal sealed record OpenAsChoice(string LogPath, string ExtractedFolder, StorageMode StorageMode, OpenAsAdvice Advice);
}
