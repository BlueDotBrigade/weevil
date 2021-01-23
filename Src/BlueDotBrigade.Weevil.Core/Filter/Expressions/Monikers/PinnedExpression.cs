namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;

	internal class PinnedExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Pinned");

		private readonly bool _isPinned;

		public PinnedExpression(string serializedExpression)
		{
			_isPinned = true;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					var canParse = bool.TryParse(Moniker.GetParameter(serializedExpression), out var userConfiguredValue);
					_isPinned = canParse ? userConfiguredValue : true;
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			var isMatch = record.Metadata.IsPinned == _isPinned;
			return isMatch;
		}
	}
}
