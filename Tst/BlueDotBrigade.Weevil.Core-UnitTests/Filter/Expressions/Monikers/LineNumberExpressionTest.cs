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
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line=25");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_RecordHasDifferentLineNumber_ReturnsFalse()
		{
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(100);

			var expression = new LineNumberExpression("@Line=25");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_ParameterHasComma_ReturnsTrue()
		{
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(1234);

			var expression = new LineNumberExpression("@Line=1,234");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_ParameterHasMultipleCommas_ReturnsTrue()
		{
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(1234567);

			var expression = new LineNumberExpression("@Line=1,234,567");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_InvalidParameter_ReturnsFalse()
		{
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line=invalid");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_NoParameter_ReturnsFalse()
		{
			IRecord record = Substitute.For<IRecord>();
			record.LineNumber.Returns(25);

			var expression = new LineNumberExpression("@Line");

			(expression.IsMatch(record, new ContentBuffer())).Should().BeFalse();
		}
	}
}
