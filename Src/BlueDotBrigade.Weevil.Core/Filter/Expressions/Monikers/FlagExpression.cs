namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class FlagExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Flagged");
		private readonly bool _isFlagged;
		private readonly IFlags _flags;

		public FlagExpression(string serializedExpression, IFlags flags)
		{
			_flags = flags;

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
			return _flags.IsFlagged(record.LineNumber) == _isFlagged;
		}
	}
}
