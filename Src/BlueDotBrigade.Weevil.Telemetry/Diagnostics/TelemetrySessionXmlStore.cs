namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Serialization;

	/// <summary>
	/// Stores telemetry sessions as XML files in a local pending directory.
	/// </summary>
	public sealed class TelemetrySessionXmlStore : ITelemetrySessionStore
	{
		private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(TelemetrySessionDto));
		private readonly string _pendingDirectoryPath;

		public TelemetrySessionXmlStore(string pendingDirectoryPath = "")
		{
			_pendingDirectoryPath = string.IsNullOrWhiteSpace(pendingDirectoryPath)
				? GetDefaultPendingDirectoryPath()
				: pendingDirectoryPath;
		}

		public void Save(TelemetrySessionDto session)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			Directory.CreateDirectory(_pendingDirectoryPath);

			var fileName = $"{session.SessionId}.xml";
			var finalPath = Path.Combine(_pendingDirectoryPath, fileName);
			var temporaryPath = $"{finalPath}.tmp";

			using (var stream = File.Open(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				Serializer.Serialize(stream, session);
			}

			File.Move(temporaryPath, finalPath, true);
		}

		public IReadOnlyList<PendingTelemetrySession> GetPendingSessions(int maxCount)
		{
			if (maxCount <= 0 || !Directory.Exists(_pendingDirectoryPath))
			{
				return Array.Empty<PendingTelemetrySession>();
			}

			var sessions = new List<PendingTelemetrySession>();

			foreach (var filePath in Directory
				.EnumerateFiles(_pendingDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.Take(maxCount))
			{
				try
				{
					using var stream = File.OpenRead(filePath);
					if (Serializer.Deserialize(stream) is TelemetrySessionDto session)
					{
						sessions.Add(new PendingTelemetrySession
						{
							FilePath = filePath,
							Session = session,
						});
					}
				}
				catch (Exception exception)
				{
					Log.Default.Write(LogSeverityType.Warning, exception, $"Telemetry XML load failed for '{filePath}'.");
				}
			}

			return sessions;
		}

		public void Delete(PendingTelemetrySession session)
		{
			if (session == null || string.IsNullOrWhiteSpace(session.FilePath))
			{
				return;
			}

			if (File.Exists(session.FilePath))
			{
				File.Delete(session.FilePath);
			}
		}

		internal static string GetDefaultPendingDirectoryPath()
		{
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"BlueDotBrigade",
				"Weevil",
				"Telemetry",
				"Pending");
		}
	}
}
