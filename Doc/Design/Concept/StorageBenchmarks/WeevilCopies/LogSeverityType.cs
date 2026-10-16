namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Represents the type of log entry that will be created.
	/// </summary>
	public enum LogSeverityType
	{
		Trace,

		/// <summary>
		/// Represents messages that reflect the inner workings of an application (e.g. testing for null).  These messages are very technical, and are written for software developers.
		/// </summary>
		Debug,

		/// <summary>
		/// Represents messages that reflect the overall state of the application (e.g loaded dependency version 1.2.3).  These messages are written for technical support, and business analysts.
		/// </summary>
		Information,

		/// <summary>
		/// Represents a recoverable event that occurred during an operation (e.g. re-connecting to a database). These messages are written for technical support, and business analysts.
		/// </summary>
		Warning,

		/// <summary>
		/// Represents an event that was fatal to an operation, but not the application.  These messages are written for technical support, and business analysts.
		/// </summary>
		Error,

		/// <summary>
		/// Represents an event that was fatal (or near fatal) to the application.  These high priority messages require the technical support team's immediate attention.
		/// </summary>
		Critical,
	}
}
