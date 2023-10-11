namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class ElapsedGreaterThanExpressionTest
	{
		[TestMethod]
		public void IsMatch_ElapsedTimeSameAsSpecifiedPeriod_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { ElapsedTime = TimeSpan.FromMilliseconds(100) });

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100");

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_ElapsedTimeLessThanSpecifiedPeriod_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Returns(new Metadata { ElapsedTime = TimeSpan.FromMilliseconds(1) });

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100");

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_ElapsedTimeGreaterThanSpecifiedPeriod_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Returns(new Metadata { ElapsedTime = TimeSpan.FromHours(1) });

			var expression = new ElapsedGreaterThanExpression("@Elapsed>100");

			Assert.IsTrue(expression.IsMatch(record));
		}
	}
}
