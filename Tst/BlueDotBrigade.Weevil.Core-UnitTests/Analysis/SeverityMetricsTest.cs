namespace BlueDotBrigade.Weevil.Analysis
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class SeverityMetricsTest
	{
		[TestMethod]
		public void AdditionOperator_()
		{
			var record = Substitute.For<IRecord>();
			record.Severity.Returns(SeverityType.Information);

			var metricsA = new SeverityMetrics();
			metricsA.Count(record); // Information=1
			metricsA.Count(record); // Information=2
			metricsA.Count(record); // Information=3

			var metricsB = new SeverityMetrics();
			metricsB.Count(record); // Information=1

			SeverityMetrics result = metricsA + metricsB;

			Assert.AreEqual(4, result.Information);
		}
	}
}
