namespace BlueDotBrigade.Weevil
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ITelemetryClient
	{
		Task SendAsync(TelemetrySession session, CancellationToken ct);

		void SendSync(TelemetrySession session);
	}
}
