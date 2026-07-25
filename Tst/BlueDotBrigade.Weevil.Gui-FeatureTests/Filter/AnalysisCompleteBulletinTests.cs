namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Collections.Generic;

	[TestClass]
	public class AnalysisCompleteBulletinTests
	{
		[TestMethod]
		public void GivenDataWithNullValues_WhenBulletinCreated_ThenNullValuesDisplayAsNA()
		{
			// Regression: Issue #931
			var data = new Dictionary<string, object>
			{
				{ "Count", 0d },
				{ "Min", null },
				{ "Max", null },
				{ "Mean", null },
				{ "Median", null },
				{ "StdDev", null },
			};

			var bulletin = new AnalysisCompleteBulletin(flaggedRecords: 0, data: data);

			bulletin.Data["Min"].Should().Be("N/A");
			bulletin.Data["Max"].Should().Be("N/A");
			bulletin.Data["Mean"].Should().Be("N/A");
			bulletin.Data["Median"].Should().Be("N/A");
			bulletin.Data["StdDev"].Should().Be("N/A");
		}

		[TestMethod]
		public void GivenDataWithDoubleValues_WhenBulletinCreated_ThenDoubleValuesAreFormattedWithThreeDecimalPlaces()
		{
			var data = new Dictionary<string, object>
			{
				{ "Mean", 2.1666666d },
			};

			var bulletin = new AnalysisCompleteBulletin(flaggedRecords: 3, data: data);

			bulletin.Data["Mean"].Should().Be("2.167");
		}
	}
}
