namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Describes a user activity signal used for telemetry usage tracking.
	/// </summary>
	public enum TelemetryActivityKind
	{
		/// <summary>
		/// A source file was opened.
		/// </summary>
		FileOpened,
		/// <summary>
		/// A filter execution was requested.
		/// </summary>
		FilterApplied,
		/// <summary>
		/// Analysis execution was requested.
		/// </summary>
		AnalysisExecuted,
		/// <summary>
		/// Records were cleared.
		/// </summary>
		RecordsCleared,
		/// <summary>
		/// The record selection changed.
		/// </summary>
		RecordSelectionChanged,
		/// <summary>
		/// A record annotation changed.
		/// </summary>
		RecordAnnotationChanged,
		/// <summary>
		/// The viewport changed.
		/// </summary>
		ViewportChanged,
		/// <summary>
		/// The graph view was opened.
		/// </summary>
		GraphOpen,
		/// <summary>
		/// The dashboard view was opened.
		/// </summary>
		DashboardOpen,
		/// <summary>
		/// A command was entered.
		/// </summary>
		CommandEntered,
		/// <summary>
		/// A command completed.
		/// </summary>
		CommandCompleted,
		/// <summary>
		/// Activity occurred but was not categorized.
		/// </summary>
		Unknown,
	}
}
