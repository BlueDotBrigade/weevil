namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	/// <summary>
	/// Lists and reads the drives, folders and containers, such as zips, that the file browser shows.
	/// </summary>
	/// <remarks>
	/// <see cref="ContainerReader"/> reads the file system and zips. A test substitutes its own, for example to make a listing fail or wait.
	/// Every member may be called from a background thread.
	/// </remarks>
	public interface IContainerReader : IDisposable
	{
		/// <summary>
		/// Says whether a file with this name is entered like a folder, as a zip is.
		/// </summary>
		bool IsContainerName(string name);

		/// <summary>
		/// Lists what a place holds, or why it could not be listed.
		/// </summary>
		ListingResult List(Location location);

		/// <summary>
		/// Says what a typed path names, or <see langword="null"/> when it names nothing.
		/// </summary>
		ItemKind? GetKind(Location location);

		/// <summary>
		/// Says whether a file exists, on disk or in a container, for example a log's sidecar beside it.
		/// </summary>
		bool Exists(Location file);

		/// <summary>
		/// Opens a file for reading, on disk or in a container. The caller disposes the stream.
		/// </summary>
		Stream OpenRead(Location file);

		/// <summary>
		/// Gives the path of a file on disk: a file on disk as it is, and a file in a container copied out to <paramref name="destinationFolder"/>,
		/// with the companions beside it that exist, such as <c>App.log.xml</c> for <c>App.log</c>.
		/// </summary>
		/// <param name="companionSuffixes">What a companion's name adds to the file's, such as <c>.xml</c>.</param>
		string Extract(Location file, IEnumerable<string> companionSuffixes, string destinationFolder);
	}
}
