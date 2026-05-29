namespace BlueDotBrigade.Weevil.Diagnostics
{
using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TelemetrySessionXmlStoreTests
{
[TestMethod]
public void GivenSavedSession_WhenPendingSessionsRequested_ThenSessionRoundTripsFromXml()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
var session = CreateSessionDto();

store.Save(session);
var pendingSessions = store.GetPendingSessions(10);

pendingSessions.Should().ContainSingle();
pendingSessions[0].FilePath.Should().Be(Path.Combine(pendingDirectory, $"{session.SessionId}.xml"));
pendingSessions[0].Session.Should().BeEquivalentTo(session);
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

[TestMethod]
public void GivenSavedSession_WhenStoredToDisk_ThenTemporaryFilesAreNotLeftBehind()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);

store.Save(CreateSessionDto());

Directory.EnumerateFiles(pendingDirectory, "*.xml").Should().ContainSingle();
Directory.EnumerateFiles(pendingDirectory, "*.tmp").Should().BeEmpty();
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

private static string CreateTemporaryDirectory()
{
var path = Path.Combine(Path.GetTempPath(), "weevil-telemetry-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(path);
return path;
}

private static TelemetrySessionDto CreateSessionDto()
{
return new TelemetrySessionDto
{
SessionId = Guid.NewGuid(),
Application = "WeevilGui.exe",
Source = "unknown",
Version = "2.12.0.0",
IsDebugging = false,
SessionStartUtc = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc),
SessionEndUtc = new DateTime(2026, 5, 28, 12, 5, 0, DateTimeKind.Utc),
SessionActiveMinutes = 5,
LogFileSizeBytes = 4096,
InstalledRamMb = 8192,
InstalledCpu = "AMD Ryzen 7",
FilterExecutionCount = 2,
GraphOpenCount = 1,
DashboardOpenCount = 1,
SchemaVersion = "2.0",
};
}
}
}
