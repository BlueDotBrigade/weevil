namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// The places in the navigation pane.
	/// </summary>
	public sealed class Place
	{
		public Place(string name, string glyph, Location location)
		{
			this.Name = name;
			this.Glyph = glyph;
			this.Location = location;
		}

		public string Name { get; }

		public string Glyph { get; }

		public Location Location { get; }

		public override string ToString() => this.Name;
	}
}
