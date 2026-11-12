namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.IO;
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
		[WorkItem(1185)]
		public void GivenEngineWithRecorder_WhenUserAppliesFilterAfterReload_ThenFilterAppliedRecordedOnce()
		{
			// After Reload, the engine recorded no telemetry at all.
			var recorder = new FakeTelemetryMetricRecorder();
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.UsingTelemetry(recorder)
				.Open();

			engine.Reload();
			recorder.Metrics.Should().BeEmpty("Reload re-applies the user's filter, which is not a new filter");

			engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);

			recorder.Metrics.Should().ContainSingle()
				.Which.Should().Be(TelemetryMetrics.FilterApplied);
		}

		[TestMethod]
		[WorkItem(1185)]
		public void GivenAReloadThatCouldNotReadTheLog_WhenReloadedAgainAndFiltered_ThenFilterAppliedRecordedOnce()
		{
			// The engine a failed Reload leaves kept the recorder, but the next Reload dropped it.
			var folder = Directory.CreateTempSubdirectory("WeevilTelemetry.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "SampleData.log");
				File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);
				var recorder = new FakeTelemetryMetricRecorder();
				IEngine engine = Engine.UsingPath(logPath).UsingTelemetry(recorder).Open();

				File.Move(logPath, $"{logPath}~");
				FluentActions.Invoking(engine.Reload).Should().Throw<IOException>();
				File.Move($"{logPath}~", logPath);
				engine.Reload();

				engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);

				recorder.Metrics.Should().ContainSingle()
					.Which.Should().Be(TelemetryMetrics.FilterApplied);
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
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
