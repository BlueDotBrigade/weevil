namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class FlagExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Flagged");
		private readonly bool _isFlagged;

		public FlagExpression(string serializedExpression)
		{
			if (Moniker.IsReferencedBy(serializedExpression))
			{
				_isFlagged = true;

				if (Moniker.HasParameter(serializedExpression))
				{
					var canParse = bool.TryParse(Moniker.GetParameter(serializedExpression), out var userConfiguredValue);
					_isFlagged = canParse ? userConfiguredValue : true;
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			return record.Metadata.IsFlagged == _isFlagged;
		}
	}
}
