namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class UiThreadExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@UiThread");

		private readonly bool _isUiRecord;

		public UiThreadExpression(string serializedExpression)
		{
			_isUiRecord = true;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					var canParse = bool.TryParse(Moniker.GetParameter(serializedExpression), out var userConfiguredValue);
					_isUiRecord = canParse ? userConfiguredValue : true;
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			return record.Metadata.WasGeneratedByUi == _isUiRecord;
		}
	}
}
