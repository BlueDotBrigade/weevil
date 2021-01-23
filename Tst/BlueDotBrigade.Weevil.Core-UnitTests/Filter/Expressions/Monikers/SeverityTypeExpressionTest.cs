namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class SeverityTypeExpressionTest
	{
		[TestMethod]
		public void IsMatch_NoMetadata_ReturnsFalse()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Metadata).Returns(new Metadata());

			var expression = new SeverityTypeExpression(SeverityType.Error);

			Assert.IsFalse(expression.IsMatch(record.Object));
		}

		[TestMethod] // TODO: Move severity to metadata? Check UI first.
		public void IsMatch_RecordHasDifferentSeverity_ReturnsFalse()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Severity).Returns(SeverityType.Debug);

			var expression = new SeverityTypeExpression(SeverityType.Error);

			Assert.IsFalse(expression.IsMatch(record.Object));
		}
	}
}
