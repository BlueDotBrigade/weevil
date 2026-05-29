namespace BlueDotBrigade.Weevil.Diagnostics
{
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TelemetryUploadWorkerTests
{
[TestMethod]
public async Task GivenPendingSession_WhenUploadSucceeds_ThenPendingXmlIsDeleted()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
store.Save(CreateSessionDto());
var client = new StubTelemetryClient(TelemetryUploadStatus.Success);
var worker = new TelemetryUploadWorker(() => client, store, retryDelay: TimeSpan.FromMilliseconds(1));

worker.TriggerUpload();
await worker.ActiveUploadTask;

Directory.EnumerateFiles(pendingDirectory, "*.xml").Should().BeEmpty();
client.UploadCallCount.Should().Be(1);
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

[TestMethod]
public async Task GivenPendingSession_WhenUploadFails_ThenPendingXmlRemains()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
store.Save(CreateSessionDto());
var client = new StubTelemetryClient(
	TelemetryUploadStatus.Failed,
	TelemetryUploadStatus.Failed,
	TelemetryUploadStatus.Failed);
var worker = new TelemetryUploadWorker(() => client, store, retryDelay: TimeSpan.FromMilliseconds(1));

worker.TriggerUpload();
await worker.ActiveUploadTask;

Directory.EnumerateFiles(pendingDirectory, "*.xml").Should().ContainSingle();
client.UploadCallCount.Should().Be(3);
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

[TestMethod]
public async Task GivenDuplicateSession_WhenUploadRuns_ThenPendingXmlIsDeleted()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
store.Save(CreateSessionDto());
var client = new StubTelemetryClient(TelemetryUploadStatus.DuplicateSession);
var worker = new TelemetryUploadWorker(() => client, store, retryDelay: TimeSpan.FromMilliseconds(1));

worker.TriggerUpload();
await worker.ActiveUploadTask;

Directory.EnumerateFiles(pendingDirectory, "*.xml").Should().BeEmpty();
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

[TestMethod]
public async Task GivenInvalidCredentials_WhenUploadRuns_ThenPendingXmlRemainsAndFurtherUploadsAreSkipped()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
store.Save(CreateSessionDto());
store.Save(CreateSessionDto());
var client = new StubTelemetryClient(TelemetryUploadStatus.InvalidCredentials, TelemetryUploadStatus.Success);
var worker = new TelemetryUploadWorker(() => client, store, retryDelay: TimeSpan.FromMilliseconds(1));

worker.TriggerUpload();
await worker.ActiveUploadTask;
worker.TriggerUpload();
await worker.ActiveUploadTask;

Directory.EnumerateFiles(pendingDirectory, "*.xml").Should().HaveCount(2);
client.UploadCallCount.Should().Be(1);
}
finally
{
Directory.Delete(pendingDirectory, true);
}
}

[TestMethod]
public async Task GivenUploadAlreadyRunning_WhenTriggerUploadCalledAgain_ThenOnlyOneUploadRuns()
{
var pendingDirectory = CreateTemporaryDirectory();

try
{
var store = new TelemetrySessionXmlStore(pendingDirectory);
store.Save(CreateSessionDto());
var uploadStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
var releaseUpload = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
var client = new BlockingTelemetryClient(uploadStarted, releaseUpload);
var worker = new TelemetryUploadWorker(() => client, store, retryDelay: TimeSpan.FromMilliseconds(1));

worker.TriggerUpload();
await uploadStarted.Task;
worker.TriggerUpload();
await Task.Delay(25);
releaseUpload.SetResult(true);
await worker.ActiveUploadTask;

client.UploadCallCount.Should().Be(1);
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
SessionStartUtc = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc),
SessionEndUtc = new DateTime(2026, 5, 28, 12, 5, 0, DateTimeKind.Utc),
SessionActiveMinutes = 5,
SchemaVersion = "2.0",
};
}

private sealed class StubTelemetryClient : ITelemetryClient
{
private readonly Queue<TelemetryUploadStatus> _statuses;

public StubTelemetryClient(params TelemetryUploadStatus[] statuses)
{
_statuses = new Queue<TelemetryUploadStatus>(statuses);
}

public int UploadCallCount { get; private set; }

public void Warmup()
{
}

public Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct)
{
UploadCallCount++;
return Task.FromResult(_statuses.Count > 0 ? _statuses.Dequeue() : TelemetryUploadStatus.Success);
}
}

private sealed class BlockingTelemetryClient : ITelemetryClient
{
private readonly TaskCompletionSource<bool> _uploadStarted;
private readonly TaskCompletionSource<bool> _releaseUpload;

public BlockingTelemetryClient(TaskCompletionSource<bool> uploadStarted, TaskCompletionSource<bool> releaseUpload)
{
_uploadStarted = uploadStarted;
_releaseUpload = releaseUpload;
}

public int UploadCallCount { get; private set; }

public void Warmup()
{
}

public async Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct)
{
UploadCallCount++;
_uploadStarted.TrySetResult(true);
await _releaseUpload.Task;
return TelemetryUploadStatus.Success;
}
}
}
}
