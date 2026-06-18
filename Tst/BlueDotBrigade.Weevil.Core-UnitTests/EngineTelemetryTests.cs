namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Analysis;
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

		[TestMethod]
		public void GivenEngineWithRecorder_WhenAnalyzerRuns_ThenPerAnalyzerMetricRecorded()
		{
			var recorder = new FakeTelemetryMetricRecorder();
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.UsingTelemetry(recorder)
				.Open();

			engine.Analyzer.Analyze(AnalysisType.StableValueRuns);

			recorder.Metrics.Should().ContainSingle()
				.Which.Should().Be("Analysis.Run.StableValueRuns");
		}

		[TestMethod]
		public void GivenPluginAnalyzerKey_WhenBuildingMetricKey_ThenKeyIsPrefixedWithAnalysisRun()
		{
			// A plugin analyzer's key flows through unchanged - no schema or code change is required.
			string key = AnalysisManager.BuildAnalysisMetricKey("Contoso.SignalQuality");

			key.Should().Be("Analysis.Run.Contoso.SignalQuality");
		}

		[TestMethod]
		public void GivenOverlyLongAnalyzerKey_WhenBuildingMetricKey_ThenKeyIsCappedToColumnLength()
		{
			string key = AnalysisManager.BuildAnalysisMetricKey(new string('x', 200));

			key.Length.Should().Be(128);
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
