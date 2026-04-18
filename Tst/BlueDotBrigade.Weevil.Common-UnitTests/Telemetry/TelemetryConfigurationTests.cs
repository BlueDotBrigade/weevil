namespace BlueDotBrigade.Weevil
{
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetryConfigurationTests
	{
		[TestMethod]
		public void GivenNullValue_WhenParseEnabledValueCalled_ThenReturnsTrue()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue(null);

			// Assert
			actual.Should().BeTrue();
		}

		[TestMethod]
		public void GivenTrueValue_WhenParseEnabledValueCalled_ThenReturnsTrue()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue("true");

			// Assert
			actual.Should().BeTrue();
		}

		[TestMethod]
		public void GivenFalseValue_WhenParseEnabledValueCalled_ThenReturnsFalse()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue("false");

			// Assert
			actual.Should().BeFalse();
		}

		[TestMethod]
		public void GivenOneValue_WhenParseEnabledValueCalled_ThenReturnsTrue()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue("1");

			// Assert
			actual.Should().BeTrue();
		}

		[TestMethod]
		public void GivenZeroValue_WhenParseEnabledValueCalled_ThenReturnsFalse()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue("0");

			// Assert
			actual.Should().BeFalse();
		}

		[TestMethod]
		public void GivenUnexpectedValue_WhenParseEnabledValueCalled_ThenReturnsTrue()
		{
			// Act
			var actual = TelemetryConfiguration.ParseEnabledValue("unexpected");

			// Assert
			actual.Should().BeTrue();
		}
	}
}
