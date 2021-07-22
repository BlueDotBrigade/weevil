namespace BlueDotBrigade.Weevil.Navigation
{
	public interface INavigate
	{
		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();

		T Using<T>() where T : INavigator;
	}
}