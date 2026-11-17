// Times Copy's way of reading selected records (record.Content, one at a time, as Gui/ClipboardHelper.cs does through
// RawRecordFormatter) against one ContentBuffer, for every Nth record of a log. Usage: CopyDiag <log> <ram|disk> <every>
using System.Diagnostics;
using System.Text;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.IO;

var mode = args[1] == "disk" ? StorageMode.Disk : StorageMode.Ram;
var every = int.Parse(args[2]);
IEngine engine = Engine.UsingPath(args[0]).UsingStorage(mode).Open();
RecordList records = engine.Records;
var positions = Enumerable.Range(0, records.Count / every).Select(i => i * every).ToArray();
var selected = positions.Select(p => records[p]).ToArray();

for (var run = 0; run < 3; run++)
{
	var stopwatch = Stopwatch.StartNew();
	var text = new StringBuilder();
	foreach (IRecord record in selected)
	{
		text.AppendLine(record.Content);
	}
	var contentSeconds = stopwatch.Elapsed.TotalSeconds;

	stopwatch.Restart();
	var buffered = new StringBuilder();
	var buffer = new ContentBuffer();
	foreach (IRecord record in selected)
	{
		buffered.Append(buffer.Read(record)).AppendLine();
	}
	var bufferSeconds = stopwatch.Elapsed.TotalSeconds;

	Console.WriteLine($"mode={args[1]} records={selected.Length:N0} (every {every}th)  record.Content: {contentSeconds:F2} s  ContentBuffer: {bufferSeconds:F2} s  same text: {text.ToString() == buffered.ToString()}");
}
