namespace BlueDotBrigade.Weevil.Analysis
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class SeverityMetricsTest
	{
		[TestMethod]
		public void AdditionOperator_()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Severity).Returns(SeverityType.Information);

			var metricsA = new SeverityMetrics();
			metricsA.Analyze(record.Object); // Information=1
			metricsA.Analyze(record.Object); // Information=2
			metricsA.Analyze(record.Object); // Information=3

			var metricsB = new SeverityMetrics();
			metricsB.Analyze(record.Object); // Information=1

			SeverityMetrics result = metricsA + metricsB;

			Assert.AreEqual(4, result.Information);
		}
	}
}
