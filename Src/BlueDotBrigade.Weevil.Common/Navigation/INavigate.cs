namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		IFindNavigator Find { get; }

		IPinNavigator Pinned { get; }

		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();
	}
}