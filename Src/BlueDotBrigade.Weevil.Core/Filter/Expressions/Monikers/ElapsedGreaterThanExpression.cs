namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class ElapsedGreaterThanExpression : IExpression
	{
		private static readonly TimeSpan DefaultElapsedTime = TimeSpan.FromSeconds(3);
		public static readonly Moniker Moniker = new Moniker("@Elapsed");

		private readonly TimeSpan _userSpecifiedValue;

		public ElapsedGreaterThanExpression(string serializedExpression)
		{
			_userSpecifiedValue = DefaultElapsedTime;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					if (int.TryParse(Moniker.GetParameter(serializedExpression), out var milliseconds))
					{
						_userSpecifiedValue = TimeSpan.FromMilliseconds(milliseconds);
					}
				}
			}
		}

		public bool IsMatch(IRecord record)
		{
			return record.Metadata.ElapsedTime > _userSpecifiedValue;
		}
	}
}
