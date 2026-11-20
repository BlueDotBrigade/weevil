namespace BlueDotBrigade.Weevil.Gui.IO
{
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// The log chosen in the Open As dialog, and how to open it (#1215).
	/// </summary>
	/// <param name="LogPath">The log on disk: where it is, or where it was extracted to from a zip, with its sidecar and index.</param>
	/// <param name="ExtractedFolder">The folder a log in a zip was extracted to, kept between opens with its sidecar and index (#1253): <see langword="null"/> for a log on disk.</param>
	/// <param name="StorageMode">RAM or Disk.</param>
	/// <param name="Advice">What Weevil suggested for the log, for the log Weevil writes.</param>
	/// <param name="DisplayPath">For a log in a zip, the path the analyst knows it by, with the zip read like a folder (#1312): otherwise <see langword="null"/>.</param>
	internal sealed record OpenAsChoice(string LogPath, string ExtractedFolder, StorageMode StorageMode, OpenAsAdvice Advice, string DisplayPath = null);
}
