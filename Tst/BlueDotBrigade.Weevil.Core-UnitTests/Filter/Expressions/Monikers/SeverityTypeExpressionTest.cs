namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class SeverityTypeExpressionTest
	{
		[TestMethod]
		public void IsMatch_RecordHasDefaultSeverity_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();

			var expression = new SeverityTypeExpression(SeverityType.Error);

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod] // TODO: Move severity to metadata? Check UI first.
		public void IsMatch_RecordHasDifferentSeverity_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			
			record.Severity.Returns(SeverityType.Debug);
			
			var expression = new SeverityTypeExpression(SeverityType.Error);

			(expression.IsMatch(record)).Should().BeFalse();
		}
	}
}
