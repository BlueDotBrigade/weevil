namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface INavigate
	{
		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();

		IRecord GoTo(int lineNumber, RecordSearchType recordSearchType);

		IRecord GoTo(string timestamp, RecordSearchType recordSearchType);

		IRecord PreviousContent(string text);
		IRecord NextContent(string text);

		IRecord PreviousPin();
		IRecord NextPin();

		IRecord PreviousComment();
		IRecord NextComment();

		IRecord PreviousFlag();
		IRecord NextFlag();
	}
}