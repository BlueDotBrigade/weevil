namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class SeverityTypeExpressionTest
	{
		[TestMethod]
		public void IsMatch_NoMetadata_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();

			record.Metadata.Returns(new Metadata());

			var expression = new SeverityTypeExpression(SeverityType.Error);

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod] // TODO: Move severity to metadata? Check UI first.
		public void IsMatch_RecordHasDifferentSeverity_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			
			record.Severity.Returns(SeverityType.Debug);
			
			var expression = new SeverityTypeExpression(SeverityType.Error);

			Assert.IsFalse(expression.IsMatch(record));
		}
	}
}
