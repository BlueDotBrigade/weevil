namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		ILineNumberNavigator LineNumber { get; }

		IFindTextNavigator Find { get; }

		IPinNavigator Pinned { get; }

		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();
	}
}