namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System.Collections.Generic;

	/// <summary>
	/// Stores telemetry sessions in a local pending outbox.
	/// </summary>
	public interface ITelemetrySessionStore
	{
		void Save(TelemetrySessionDto session);

		IReadOnlyList<PendingTelemetrySession> GetPendingSessions(int maxCount);

		void Delete(PendingTelemetrySession session);
	}
}
