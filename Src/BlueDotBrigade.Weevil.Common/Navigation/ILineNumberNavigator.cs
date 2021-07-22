namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ILineNumberNavigator : INavigator
	{
		/// <summary>
		/// Returns the index of the <see cref="IRecord"/> that matches the provided line number.
		/// </summary>
		IRecord Find(int lineNumber);
	}
}
