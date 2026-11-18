// Counts what one filter pass allocates, by type and heap, through the runtime's AllocationTick events (about one per 100 KB).
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;

var path = args[0];
var mode = args[1] == "disk" ? BlueDotBrigade.Weevil.IO.StorageMode.Disk : BlueDotBrigade.Weevil.IO.StorageMode.Ram;
IEngine engine = Engine.UsingPath(path).UsingStorage(mode).Open();
engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("timeout")); // warm-up

using var listener = new AllocListener();
var before = GC.GetTotalAllocatedBytes(true);
var gen2 = GC.CollectionCount(2);
engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("timeout"));
var total = GC.GetTotalAllocatedBytes(true) - before;
Thread.Sleep(500);
Console.WriteLine($"mode={args[1]} allocated={total / 1e6:F0} MB gen2 collections={GC.CollectionCount(2) - gen2} results={engine.Filter.Results.Count}");
foreach (var kv in listener.Totals.OrderByDescending(kv => kv.Value).Take(8))
	Console.WriteLine($"  {kv.Value / 1e6,8:F0} MB  {kv.Key}");

sealed class AllocListener : EventListener
{
	public readonly ConcurrentDictionary<string, long> Totals = new();
	protected override void OnEventSourceCreated(EventSource source)
	{
		if (source.Name == "Microsoft-Windows-DotNETRuntime")
			EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1); // GC keyword
	}
	protected override void OnEventWritten(EventWrittenEventArgs e)
	{
		if (e.EventName != "GCAllocationTick_V4" && e.EventName != "GCAllocationTick_V3") return;
		var kind = Convert.ToInt32(e.Payload![e.PayloadNames!.IndexOf("AllocationKind")]) switch { 0 => "SOH", 1 => "LOH", _ => "POH" };
		var type = (string)e.Payload[e.PayloadNames.IndexOf("TypeName")]!;
		var amount = Convert.ToInt64(e.Payload[e.PayloadNames.IndexOf("AllocationAmount64")]);
		Totals.AddOrUpdate($"{kind} {type}", amount, (_, v) => v + amount);
	}
}
