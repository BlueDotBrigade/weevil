namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class PinnedExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Pinned");

		private readonly bool _isPinned;
		private readonly IRecordState _state;

		public PinnedExpression(string serializedExpression, IRecordState state)
		{
			_state = state;
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
			var isMatch = _state.IsPinned(record) == _isPinned;
			return isMatch;
		}
	}
}
