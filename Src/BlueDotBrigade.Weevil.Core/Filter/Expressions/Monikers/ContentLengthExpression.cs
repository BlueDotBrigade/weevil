namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using BlueDotBrigade.Weevil.Filter.Expressions;
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

		public bool IsMatch(IRecord record, ContentBuffer content)
		{
			// The only built-in moniker that reads the content.
			ReadOnlySpan<char> text = content.Read(record);

			return !text.IsWhiteSpace() && text.Length >= _contentLength;
		}
	}
}
