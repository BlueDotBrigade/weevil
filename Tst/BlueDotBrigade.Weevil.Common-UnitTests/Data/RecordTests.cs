namespace BlueDotBrigade.Weevil.Data
{
	using System;

	[TestClass]
	public class RecordTests
	{
		[TestMethod]
		public void GivenEverySeverityAndFact_WhenRecordCreated_ThenEachIsKept()
		{
			foreach (SeverityType severity in Enum.GetValues<SeverityType>())
			{
				foreach (var isMultiLine in new[] { false, true })
				{
					foreach (var wasGeneratedByUi in new[] { false, true })
					{
						var record = new Record(1, DateTime.Now, severity, "content", new Metadata(), isMultiLine, wasGeneratedByUi);

						record.Severity.Should().Be(severity);
						record.IsMultiLine.Should().Be(isMultiLine);
						record.WasGeneratedByUi.Should().Be(wasGeneratedByUi);
					}
				}
			}
		}

		[TestMethod]
		public void GivenRecordWithoutFacts_WhenCreated_ThenItIsSingleLineAndNotFromTheUiThread()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Warning, "content");

			record.Severity.Should().Be(SeverityType.Warning);
			record.IsMultiLine.Should().BeFalse();
			record.WasGeneratedByUi.Should().BeFalse();
		}

		[TestMethod]
		public void GivenUnknownSeverity_WhenRecordCreated_ThenThrows()
		{
			Action create = () => new Record(1, DateTime.Now, (SeverityType)8, "content");

			create.Should().Throw<ArgumentOutOfRangeException>();
		}
	}
}
