namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		IFindNavigator LineNumber { get; }

		IFindNavigator Find { get; }

		IPinNavigator Pinned { get; }

		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();
	}
}