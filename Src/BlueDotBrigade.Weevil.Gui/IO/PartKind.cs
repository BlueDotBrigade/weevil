namespace BlueDotBrigade.Weevil.Gui.IO
{
	/// <summary>
	/// How much of a log the Open As dialog opens: one way at a time (decision 5 of the #1157 plan).
	/// </summary>
	internal enum PartKind
	{
		Whole = 0,
		Section = 1,
		Region = 2,
		Lines = 3,
	}
}
