// Times the open paths around the saved index (.idx): first open with and without saving it,
// reopen in the same storage mode, and reopen in the other mode. Usage: ReopenDiag <log>
using System.Diagnostics;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.IO;

var log = Path.GetFullPath(args[0]);
var idx = log + ".idx";
// A subfolder: a log directly in the temporary folder is not recognised as temporary (FileHelper.IsInTemporaryDirectory compares "/tmp" with "/tmp/").
var tempFolder = System.IO.Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "WeevilAudit")).FullName;
var tempCopy = Path.Combine(tempFolder, Path.GetFileName(log));

double Open(string path, StorageMode mode)
{
	var stopwatch = Stopwatch.StartNew();
	IEngine engine = Engine.UsingPath(path).UsingStorage(mode).Open();
	var seconds = stopwatch.Elapsed.TotalSeconds;
	var records = engine.Records.Count;
	(engine as IDisposable)?.Dispose();
	engine = null!;
	GC.Collect();
	GC.WaitForPendingFinalizers();
	GC.Collect();
	Console.WriteLine($"{seconds,7:F2} s  {records:N0} records");
	return seconds;
}

void Step(string name, string path, StorageMode mode)
{
	Console.Write($"{name,-58}");
	Open(path, mode);
}

System.IO.File.Delete(idx);
System.IO.File.Copy(log, tempCopy, overwrite: true);
Step("first open, RAM, in the temporary folder (no .idx)", tempCopy, StorageMode.Ram);
Step("first open, RAM, beside the log (writes the .idx)", log, StorageMode.Ram);
Console.WriteLine($"{"  .idx size",-58}{new System.IO.FileInfo(idx).Length / 1e6,7:F0} MB");
Step("reopen, RAM (same mode)", log, StorageMode.Ram);
Step("reopen, Disk (other mode: U1-7)", log, StorageMode.Disk);
Step("reopen, Disk (same mode)", log, StorageMode.Disk);
Step("reopen, RAM (other mode again)", log, StorageMode.Ram);
System.IO.File.Delete(tempCopy);
Console.WriteLine($"{"  .idx saved in the temporary subfolder?",-58}{System.IO.File.Exists(tempCopy + ".idx")}");
