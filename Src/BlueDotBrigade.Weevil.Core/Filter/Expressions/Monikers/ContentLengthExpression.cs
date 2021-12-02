namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;

	internal class ContentLengthExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@ContentLength");

		private readonly int _contentLength;

		public ContentLengthExpression(string serializedExpression)
		{
			_contentLength = int.MaxValue;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					var canParse = int.TryParse(Moniker.GetParameter(serializedExpression), out var userConfiguredValue);
					_contentLength = canParse ? userConfiguredValue : 0;
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			var isMatch = false;
			if (record.HasContent)
			{
				if (record.Content.Length >= _contentLength)
				{
					isMatch = true;
				}
			}

			return isMatch;
		}
	}
}
