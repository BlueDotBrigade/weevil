namespace BlueDotBrigade.Windows.Dialogs.Storage
{
	using System;
	using System.IO;

	public enum ItemKind
	{
		Drive,
		Folder,

		/// <summary>
		/// A file that holds others, such as a zip: the browser enters it like a folder.
		/// </summary>
		Container,

		File,
	}

	/// <summary>
	/// A drive, folder, zip or file, as the browser lists it.
	/// </summary>
	public sealed class BrowserItem
	{
		public BrowserItem(Location location, string name, ItemKind kind, long? size = null, DateTime? modified = null, bool isOnlineOnly = false)
		{
			this.Location = location ?? throw new ArgumentNullException(nameof(location));
			this.Name = name;
			this.Kind = kind;
			this.Size = size;
			this.Modified = modified;
			this.IsOnlineOnly = isOnlineOnly;
		}

		/// <summary>
		/// Where the item is. For a zip, the zip as a file: <see cref="Open"/> gives its top.
		/// </summary>
		public Location Location { get; }

		public string Name { get; }

		public ItemKind Kind { get; }

		public long? Size { get; }

		public DateTime? Modified { get; }

		/// <summary>
		/// A cloud file, such as OneDrive's, whose contents are not on this computer: reading it downloads it.
		/// </summary>
		public bool IsOnlineOnly { get; }

		public bool IsFile => this.Kind == ItemKind.File;

		/// <summary>
		/// The location the browser shows when the analyst opens the item: a folder's, or a zip's top.
		/// </summary>
		public Location Open => this.Kind == ItemKind.Container ? this.Location.EnterContainer() : this.Location;

		public string TypeName => this.Kind switch
		{
			ItemKind.Drive => "Drive",
			ItemKind.Folder => "File folder",
			ItemKind.Container => "Compressed folder",
			_ => Path.GetExtension(this.Name) is { Length: > 1 } extension ? $"{extension.Substring(1).ToUpperInvariant()} file" : "File",
		};

		/// <summary>
		/// The name, which screen readers announce for the item's row.
		/// </summary>
		public override string ToString() => this.Name;
	}
}
