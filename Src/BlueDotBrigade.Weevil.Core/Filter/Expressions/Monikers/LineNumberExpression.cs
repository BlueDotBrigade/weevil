namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class LineNumberExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Line");

		private readonly int _lineNumber;
		private readonly bool _isValid;

		public LineNumberExpression(string serializedExpression)
		{
			_lineNumber = -1;
			_isValid = false;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					var parameter = Moniker.GetParameter(serializedExpression);
					// Remove commas from the parameter to support comma-separated integers like "1,234"
					var cleanedParameter = parameter.Replace(",", string.Empty);
					
					if (int.TryParse(cleanedParameter, out var lineNumber))
					{
						_lineNumber = lineNumber;
						_isValid = true;
					}
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			return _isValid && record.LineNumber == _lineNumber;
		}
	}
}
