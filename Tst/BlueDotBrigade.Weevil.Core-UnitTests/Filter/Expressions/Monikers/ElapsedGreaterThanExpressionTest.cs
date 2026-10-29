namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System.Collections.Immutable;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ElapsedGreaterThanExpressionTest
	{
		[TestMethod]
		public void IsMatch_ElapsedTimeSameAsSpecifiedPeriod_ReturnsFalse()
		{
			ImmutableArray<IRecord> records = CreateRecordsMillisecondsApart(100);

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100", ElapsedTimes.Calculate(records));

			(expression.IsMatch(records[1], new ContentBuffer())).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_ElapsedTimeLessThanSpecifiedPeriod_ReturnsFalse()
		{
			ImmutableArray<IRecord> records = CreateRecordsMillisecondsApart(1);

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100", ElapsedTimes.Calculate(records));

			(expression.IsMatch(records[1], new ContentBuffer())).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_ElapsedTimeGreaterThanSpecifiedPeriod_ReturnsTrue()
		{
			ImmutableArray<IRecord> records = CreateRecordsMillisecondsApart(3_600_000);

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100", ElapsedTimes.Calculate(records));

			(expression.IsMatch(records[1], new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void GivenRecordThePreviousFilterHid_WhenMatched_ThenItDoesNotMatch()
		{
			ImmutableArray<IRecord> records = CreateRecordsMillisecondsApart(3_600_000);
			ImmutableArray<IRecord> previousResults = ImmutableArray.Create(records[0]);

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100", ElapsedTimes.Calculate(previousResults));

			(expression.IsMatch(records[1], new ContentBuffer())).Should().BeFalse();
		}

		private static ImmutableArray<IRecord> CreateRecordsMillisecondsApart(int milliseconds)
		{
			return R.Create()
				.WithCreatedAt(1, new System.DateTime(2026, 10, 22, 10, 0, 0))
				.WithCreatedAt(2, new System.DateTime(2026, 10, 22, 10, 0, 0).AddMilliseconds(milliseconds))
				.GetRecords();
		}
	}
}
