namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;

	public interface IBookendManager
	{
		ImmutableArray<Bookend> Bookends { get; }

		void CreateFromSelection();

		void Clear();

		bool Clear(int recordIndex);

		void MarkEnd(int lineNumber);

		void MarkStart(int lineNumber);
	}
}