namespace BlueDotBrigade.Weevil.Filter
{
	internal class DefaultAliasExpander : IStaticAliasExpander
	{
		public string[] Expand(string[] expressions)
		{
			return expressions;
		}

		public string Expand(string filter)
		{
			return filter;
		}
	}
}