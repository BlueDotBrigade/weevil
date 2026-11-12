namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// An insight report, built once by <see cref="InsightReportGenerator.Create"/> and then rendered: as Markdown for people, or for programs (#837).
	/// </summary>
	/// <remarks>
	/// It is a contract with the programs that read it. A change to what a property means, or a property removed, needs a new <see cref="SchemaVersion"/>.
	/// It holds no record text: building it never reads the log.
	/// Scopes differ: <see cref="RecordCount"/> and <see cref="FileSize"/> describe the log as it was opened, <see cref="Metrics"/> the filter's results when the report was built,
	/// and <see cref="Insights"/> and the period what the caller analyzed.
	/// </remarks>
	public sealed record InsightReport
	{
		/// <summary>
		/// The version of this report's shape that <see cref="InsightReportGenerator"/> builds.
		/// </summary>
		public const int CurrentSchemaVersion = 1;

		public int SchemaVersion { get; init; } = CurrentSchemaVersion;

		/// <summary>
		/// The log's file name, without its folder.
		/// </summary>
		public required string SourceFileName { get; init; }

		/// <summary>
		/// The log's context, such as the version of the application that wrote it, in the order it was given. Empty when it has none.
		/// </summary>
		public required IReadOnlyDictionary<string, string> Context { get; init; }

		/// <summary>
		/// The version of Weevil that built the report.
		/// </summary>
		public required string WeevilVersion { get; init; }

		public required DateTimeOffset GeneratedAt { get; init; }

		/// <summary>
		/// The start of the period analyzed, or <see langword="null"/> when it is not known.
		/// </summary>
		public DateTime? From { get; init; }

		/// <summary>
		/// The end of the period analyzed, or <see langword="null"/> when it is not known.
		/// </summary>
		public DateTime? To { get; init; }

		public required int RecordCount { get; init; }

		/// <summary>
		/// The log's size, in bytes.
		/// </summary>
		public required long FileSize { get; init; }

		/// <summary>
		/// The filter's metrics when the report was built, such as the number of records of each severity, in the order the filter gives them.
		/// </summary>
		public required ImmutableArray<InsightReportMetric> Metrics { get; init; }

		/// <summary>
		/// Every insight, each with whether it needs attention. A reader that wants only the problems filters by <see cref="InsightReportItem.IsAttentionRequired"/>.
		/// </summary>
		public required ImmutableArray<InsightReportItem> Insights { get; init; }
	}
}
