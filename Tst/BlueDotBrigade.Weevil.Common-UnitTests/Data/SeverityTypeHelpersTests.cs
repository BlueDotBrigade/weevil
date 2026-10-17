namespace BlueDotBrigade.Weevil.Data
{
	using System;

	[TestClass]
	public class SeverityTypeHelpersTests
	{
		[TestMethod]
		public void GivenKnownSeverityInSpan_WhenParsing_ThenReturnsSeverity()
		{
			var line = "Warning\tmessage".AsSpan();

			var isKnown = SeverityTypeHelpers.TryParse(line[..7], out SeverityType severityType);

			isKnown.Should().BeTrue();
			severityType.Should().Be(SeverityType.Warning);
		}

		[TestMethod]
		public void GivenSeverityAliasInSpan_WhenParsing_ThenReturnsSeverity()
		{
			SeverityTypeHelpers.TryParse("Fatal".AsSpan(), out SeverityType severityType).Should().BeTrue();

			severityType.Should().Be(SeverityType.Critical);
		}

		[TestMethod]
		public void GivenUnknownSeverityInSpan_WhenParsing_ThenReturnsInformation()
		{
			SeverityTypeHelpers.TryParse("Notice".AsSpan(), out SeverityType severityType).Should().BeFalse();

			severityType.Should().Be(SeverityType.Information);
		}

		[TestMethod]
		public void GivenDifferentCaseInSpan_WhenParsing_ThenMatchesStringOverload()
		{
			var spanResult = SeverityTypeHelpers.TryParse("warning".AsSpan(), out SeverityType spanSeverity);
			var stringResult = SeverityTypeHelpers.TryParse("warning", out SeverityType stringSeverity);

			spanResult.Should().Be(stringResult);
			spanSeverity.Should().Be(stringSeverity);
		}
	}
}
