namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Describes the result of attempting to upload a telemetry session.
	/// </summary>
	public enum TelemetryUploadStatus
	{
		Success,
		Disabled,
		DuplicateSession,
		InvalidCredentials,
		Failed,
	}
}
