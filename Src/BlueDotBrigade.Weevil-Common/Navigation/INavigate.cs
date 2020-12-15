namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		IPinNavigator Pinned { get; }

		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();
	}
}