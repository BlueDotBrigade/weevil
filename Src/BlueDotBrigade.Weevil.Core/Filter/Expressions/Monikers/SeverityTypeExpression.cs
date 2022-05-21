namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class SeverityTypeExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Severity");

		private readonly Func<SeverityType, bool> _isMatchDelegate;

		public SeverityTypeExpression(SeverityType severityType)
		{
			_isMatchDelegate = s => s == severityType;
		}

		public SeverityTypeExpression(string serializedExpression)
		{
			_isMatchDelegate = s => false;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					if (SeverityTypeHelpers.TryParse(Moniker.GetParameter(serializedExpression), out SeverityType severityType))
					{
						_isMatchDelegate = s => s == severityType;
					}
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			return _isMatchDelegate(record.Severity);
		}
	}
}
