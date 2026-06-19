namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Canonical telemetry metric keys, shared across producers so that names stay consistent.
	/// </summary>
	/// <remarks>
	/// Metric keys are stored verbatim in <c>dbo.telemetry_session_metric.metric_key</c> (max 128 chars).
	/// </remarks>
	public static class TelemetryMetrics
	{
		// Shared Core operations (emitted by BlueDotBrigade.Weevil.Core).
		public const string FilterApplied = "Filter.Applied";

		public const string NavigationGoToLine = "Navigation.GoToLine";
		public const string NavigationGoToTimestamp = "Navigation.GoToTimestamp";
		public const string NavigationFindNextContent = "Navigation.FindNextContent";
		public const string NavigationFindPreviousContent = "Navigation.FindPreviousContent";

		/// <summary>
		/// Prefix for per-analyzer metrics. The analyzer's key is appended at runtime,
		/// e.g. <c>Analysis.Run.DetectData</c>.
		/// </summary>
		public const string AnalysisRun = "Analysis.Run";

		// Host-specific operations (emitted by GUI / CLI).
		public const string InsightOpened = "Insight.Opened";

		public const string HelpOpened = "Help.Opened";
		public const string DashboardOpened = "Dashboard.Opened";
		public const string GraphOpened = "Graph.Opened";

		public const string CliFilterCommand = "Cli.Command.Filter";
		public const string CliInsightCommand = "Cli.Command.Insight";
	}
}
