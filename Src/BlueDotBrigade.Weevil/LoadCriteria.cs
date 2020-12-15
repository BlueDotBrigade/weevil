namespace BlueDotBrigade.Weevil
{
	using Filter;

	internal class LoadCriteria
	{
		public int MaxRecords { get; set; }
		public bool OnlyNewRecords { get; set; }
		public IFilterCriteria FilterCriteria { get; private set; }
	}
}
