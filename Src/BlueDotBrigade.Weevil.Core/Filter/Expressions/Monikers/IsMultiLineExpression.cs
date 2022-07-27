namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class IsMultiLineExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@IsMultiLine");

		private readonly bool _isMultiLine;

		public IsMultiLineExpression(string serializedExpression)
		{
			_isMultiLine = true;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					var canParse = bool.TryParse(Moniker.GetParameter(serializedExpression), out var userConfiguredValue);
					_isMultiLine = canParse ? userConfiguredValue : true;
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			var isMatch = record.Metadata.IsMultiLine == _isMultiLine;
			return isMatch;
		}
	}
}
