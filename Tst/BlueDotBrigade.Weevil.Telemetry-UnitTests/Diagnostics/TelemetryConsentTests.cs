namespace BlueDotBrigade.Weevil.Diagnostics
{
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetryConsentTests
	{
		[TestMethod]
		[DataRow("1")]
		[DataRow("true")]
		[DataRow("TRUE")]
		[DataRow("TrUe")]
		public void GivenSupportedTelemetryConsentValue_WhenIsEnabledCalled_ThenReturnsTrue(string value)
		{
			// Regression: Issue #919
			TelemetryConsent.IsEnabled(value).Should().BeTrue();
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow(" ")]
		[DataRow("0")]
		[DataRow("false")]
		[DataRow("FALSE")]
		[DataRow("telemetry")]
		[DataRow(" true ")]
		public void GivenUnsupportedTelemetryConsentValue_WhenIsEnabledCalled_ThenReturnsFalse(string value)
		{
			// Regression: Issue #919
			TelemetryConsent.IsEnabled(value).Should().BeFalse();
		}
	}
}
