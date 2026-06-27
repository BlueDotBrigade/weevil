namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Triggers background upload of locally persisted telemetry sessions.
	/// </summary>
	public interface ITelemetryUploadWorker
	{
		void TriggerUpload();
	}
}
