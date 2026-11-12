namespace BlueDotBrigade.Weevil.IO
{
	/// <summary>
	/// The format that the command line's <c>--output-as</c> option chose (#837).
	/// </summary>
	public enum OutputFormat
	{
		Markdown,
		PlainText,

		/// <summary>
		/// A document for programs, which a command renders from its model, such as the insight report.
		/// </summary>
		Json,
	}
}
