namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Text.Encodings.Web;
	using System.Text.Json;

	/// <summary>
	/// Renders an <see cref="InsightReport"/> as JSON, for programs (#837).
	/// </summary>
	/// <remarks>
	/// The JSON is a contract, described in <c>Doc/Design/InsightReport.md</c>: camelCase names, ISO 8601 times,
	/// <see langword="null"/> for a time that is not known, and the report's schema version.
	/// </remarks>
	public sealed class JsonInsightReportRenderer
	{
		private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,

			// Text such as "Σ" or "'" stays readable. The JSON is written to a console or a file, never into a web page.
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		public string Render(InsightReport report)
		{
			ArgumentNullException.ThrowIfNull(report);

			return JsonSerializer.Serialize(report, Options);
		}
	}
}
