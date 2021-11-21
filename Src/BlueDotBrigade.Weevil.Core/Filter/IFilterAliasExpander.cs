namespace BlueDotBrigade.Weevil.Filter
{
	public interface IFilterAliasExpander
	{
		/// <summary>
		/// Replaces known aliases with their fully qualified representation.
		/// </summary>
		/// <param name="expressions">Represents a list of expressions that may, or may not, reference a known alias.</param>
		string[] Expand(string[] expressions);

		string Expand(string filter);
	}
}