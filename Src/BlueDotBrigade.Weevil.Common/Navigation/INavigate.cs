namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();

		T By<T>() where T : INavigator;
	}
}