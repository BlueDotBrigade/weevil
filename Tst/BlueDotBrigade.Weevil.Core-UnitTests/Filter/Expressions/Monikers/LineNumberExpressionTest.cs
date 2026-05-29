namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class LineNumberExpressionTest
	{
		[TestMethod]
		public void IsMatch_RecordHasMatchingLineNumber_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line=25");

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_RecordHasDifferentLineNumber_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(100);

			var expression = new LineNumberExpression("@Line=25");

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_ParameterHasComma_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(1234);

			var expression = new LineNumberExpression("@Line=1,234");

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_ParameterHasMultipleCommas_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(1234567);

			var expression = new LineNumberExpression("@Line=1,234,567");

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_InvalidParameter_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line=invalid");

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_NoParameter_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line");

			(expression.IsMatch(record)).Should().BeFalse();
		}
	}
}
