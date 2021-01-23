namespace BlueDotBrigade.Weevil.Navigation
{
	public interface ISection
	{
		int LineNumber { get; set; }
		int Level { get; set; }
		string Name { get; set; }
	}
}