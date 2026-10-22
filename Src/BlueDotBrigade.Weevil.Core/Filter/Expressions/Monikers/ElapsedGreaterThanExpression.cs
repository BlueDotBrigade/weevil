namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	/// <summary>
	/// Matches records whose elapsed time is greater than the given number of milliseconds, 3 seconds by default.
	/// </summary>
	/// <remarks>
	/// It compares the elapsed times of the results the previous filter showed, which the view passes in (#1016).
	/// A record the previous filter hid has none, so it never matches.
	/// </remarks>
	internal class ElapsedGreaterThanExpression : IExpression
	{
		private static readonly TimeSpan DefaultElapsedTime = TimeSpan.FromSeconds(3);
		public static readonly Moniker Moniker = new Moniker("@Elapsed");

		private readonly TimeSpan _userSpecifiedValue;
		private readonly ElapsedTimes _previousElapsedTimes;

		public ElapsedGreaterThanExpression(string serializedExpression, ElapsedTimes previousElapsedTimes)
		{
			_userSpecifiedValue = DefaultElapsedTime;
			_previousElapsedTimes = previousElapsedTimes ?? throw new ArgumentNullException(nameof(previousElapsedTimes));

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
			return _previousElapsedTimes.Get(record.LineNumber) > _userSpecifiedValue;
		}
	}
}
