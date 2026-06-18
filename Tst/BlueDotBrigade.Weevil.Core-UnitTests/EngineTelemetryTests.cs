namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Navigation;

	[TestClass]
	public class EngineTelemetryTests
	{
		[TestMethod]
		public void GivenEngineWithRecorder_WhenOpened_ThenConstructionRecordsNoMetrics()
		{
			// The construction-time default filter must not be counted as user activity.
			var recorder = new FakeTelemetryMetricRecorder();

			Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.UsingTelemetry(recorder)
				.Open();

			recorder.Metrics.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenEngineWithRecorder_WhenUserAppliesFilter_ThenFilterAppliedRecordedOnce()
		{
			var recorder = new FakeTelemetryMetricRecorder();
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.UsingTelemetry(recorder)
				.Open();

			engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);

			recorder.Metrics.Should().ContainSingle()
				.Which.Should().Be(TelemetryMetrics.FilterApplied);
		}

		[TestMethod]
		public void GivenEngineWithRecorder_WhenUserGoesToLine_ThenNavigationGoToLineRecorded()
		{
			var recorder = new FakeTelemetryMetricRecorder();
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.UsingTelemetry(recorder)
				.Open();

			engine.Navigate.GoTo(1, RecordSearchType.ExactMatch);

			recorder.Metrics.Should().ContainSingle()
				.Which.Should().Be(TelemetryMetrics.NavigationGoToLine);
		}

		private sealed class FakeTelemetryMetricRecorder : ITelemetryMetricRecorder
		{
			public List<string> Metrics { get; } = new List<string>();

			public void Increment(string metricKey)
			{
				Metrics.Add(metricKey);
			}
		}
	}
}
