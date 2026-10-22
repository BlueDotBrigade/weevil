// Checks the window-free parts of the #1014 proof of concept: selection ranges, IndexOf, and the comment rules.
// Run it with `dotnet run` from this folder; it prints each failure and exits with 1 if there is any.

#nullable enable

namespace Weevil.VirtualListPoC.SelfTest;

using System.Diagnostics;

internal static class Program
{
	private static int _failures;

	public static int Main()
	{
		CheckSyntheticLog();
		CheckRangeSelection();
		CheckRemap();
		CheckVirtualRowList();
		CheckAnnotations();
		CheckRemapAtScale();

		Console.WriteLine(_failures == 0 ? "All checks passed." : $"{_failures} check(s) failed.");
		return _failures == 0 ? 0 : 1;
	}

	private static void CheckSyntheticLog()
	{
		Expect(SyntheticLog.GetLineNumber(0) == 1, "record 0 is on line 1");
		Expect(SyntheticLog.GetLineNumber(25) == 26, "the first call stack starts on line 26");
		Expect(SyntheticLog.GetLineNumber(26) == 32, "the record after a 6-line call stack starts 6 lines later");
		Expect(SyntheticLog.GetLineNumber(76) == 87, "two call stacks add 10 lines");
		Expect(SyntheticLog.GetContent(25).Split("\r\n").Length == 6, "a call stack has 6 lines");

		var previous = 0;
		for (var record = 0; record < 10_000; record++)
		{
			var line = SyntheticLog.GetLineNumber(record);
			var lines = SyntheticLog.GetContent(record).Split("\r\n").Length;
			Expect(record == 0 || line == previous, $"record {record} starts where the previous one ends");
			previous = line + lines;
		}

		var warnings = SyntheticLog.Filter(FilterKind.WarningsAndAbove, 100_000);
		Expect(warnings.All(r => SyntheticLog.GetSeverity(r) >= SeverityType.Warning), "the warnings filter keeps only warnings and above");
		Expect(SyntheticLog.Filter(FilterKind.EveryTenthRecord, 100).SequenceEqual(Enumerable.Range(0, 10).Select(i => i * 10)), "every 10th record");
	}

	private static void CheckRangeSelection()
	{
		var selection = new RangeSelection();
		selection.Add(10, 20);
		selection.Add(30, 40);
		Expect(selection.RangeCount == 2 && selection.Count == 22, "two separate ranges");

		selection.Add(21, 29);
		Expect(selection.RangeCount == 1 && selection.Count == 31, "a range that touches both merges them");

		selection.Toggle(25);
		Expect(selection.RangeCount == 2 && selection.Count == 30 && !selection.Contains(25), "Ctrl+click in the middle splits the range");

		selection.Toggle(25);
		Expect(selection.RangeCount == 1 && selection.Contains(25), "Ctrl+click again joins it");

		selection.Remove(5, 12);
		Expect(selection.First == 13 && selection.Count == 28, "removing the start of a range");

		selection.Add(50, 45);
		Expect(selection.Contains(47) && selection.RangeCount == 2, "a range given backwards, as Shift+Up makes");

		selection.SelectAll(100_000_000);
		Expect(selection.RangeCount == 1 && selection.Count == 100_000_000, "Ctrl+A is one range");

		selection.SelectOnly(7);
		Expect(selection.RangeCount == 1 && selection.Count == 1 && selection.Contains(7) && !selection.Contains(8), "a click selects one row");

		var random = new Random(1014);
		var expected = new SortedSet<int>();
		selection.Clear();
		for (var i = 0; i < 5_000; i++)
		{
			var a = random.Next(0, 2_000);
			var b = a + random.Next(0, 20);
			switch (random.Next(3))
			{
				case 0:
					selection.Add(a, b);
					expected.UnionWith(Enumerable.Range(a, b - a + 1));
					break;
				case 1:
					selection.Remove(a, b);
					expected.ExceptWith(Enumerable.Range(a, b - a + 1));
					break;
				default:
					selection.Toggle(a);
					if (!expected.Remove(a))
					{
						expected.Add(a);
					}

					break;
			}
		}

		Expect(selection.EnumeratePositions().SequenceEqual(expected), "5,000 random changes match a set of positions");
		Expect(selection.Count == expected.Count, "the count matches");
		Expect(selection.Ranges.Zip(selection.Ranges.Skip(1)).All(p => p.First.Last + 1 < p.Second.First), "ranges never overlap or touch");
	}

	private static void CheckRemap()
	{
		var all = Enumerable.Range(0, 100).ToArray();
		var even = all.Where(r => r % 2 == 0).ToArray();
		var tenth = all.Where(r => r % 10 == 0).ToArray();

		var selection = new RangeSelection();
		selection.SelectAll(all.Length);
		var remapped = RangeSelection.Remap(selection, all, even);
		Expect(remapped.Count == 50 && remapped.RangeCount == 1, "every row selected, then a narrower filter: all of its rows stay selected");

		remapped = RangeSelection.Remap(remapped, even, tenth);
		Expect(remapped.Count == 10 && remapped.RangeCount == 1, "a scattered selection, then a narrower filter");

		remapped = RangeSelection.Remap(remapped, tenth, all);
		Expect(remapped.Count == 10 && remapped.RangeCount == 10, "then no filter: only the 10 selected records, as 10 ranges");
		Expect(remapped.EnumeratePositions().SequenceEqual(tenth), "and they are the right ones");

		selection.Clear();
		selection.Add(1, 3);
		remapped = RangeSelection.Remap(selection, even, all);
		Expect(remapped.EnumeratePositions().SequenceEqual(new[] { 2, 4, 6 }), "records shown by the new filter only are not selected");

		selection.Clear();
		selection.Add(0, 0);
		remapped = RangeSelection.Remap(selection, all, even.Skip(1).ToArray());
		Expect(remapped.Count == 0, "a selected record the new filter hides leaves the selection");
	}

	private static void CheckVirtualRowList()
	{
		var annotations = new AnnotationStore();
		var list = new VirtualRowList(annotations, position => position == 3);
		list.Reset(SyntheticLog.Filter(FilterKind.EverySecondRecord, 1_000));

		var row = (RowViewModel)list[3]!;
		Expect(row.RecordNumber == 6 && row.IsSelected, "the row at position 3 is record 6, and selected");
		Expect(ReferenceEquals(row, list[3]), "the same row comes back while it is alive");
		Expect(list.IndexOf(row) == 3, "IndexOf finds it");
		Expect(list.IndexOf(new RowViewModel(0, 7, TimeSpan.Zero, false, annotations)) == -1, "IndexOf of a hidden record is -1");
		Expect(row.Equals(new RowViewModel(99, 6, TimeSpan.Zero, false, annotations)), "rows are equal by line number");
		Expect(row.ElapsedTime == SyntheticLog.GetTimestamp(6) - SyntheticLog.GetTimestamp(4), "elapsed time is from the previous result");
		Expect(list.Counters.Enumerations == 0, "nothing enumerated the list");

		GC.KeepAlive(row);
	}

	private static void CheckAnnotations()
	{
		var annotations = new AnnotationStore();
		var list = new VirtualRowList(annotations, _ => false);
		list.Reset(SyntheticLog.Filter(FilterKind.AllRecords, 100));
		RowViewModel row = list.GetRow(10);

		var events = new List<AnnotationChangedEventArgs>();
		annotations.Changed += (_, e) => events.Add(e);

		row.Comment = "abc";
		Expect(annotations.GetComment(row.LineNumber) == "abc" && annotations.IsPinned(row.LineNumber), "a comment is saved and pins the record");
		Expect(events.Count == 1 && ReferenceEquals(events[0].Source, row), "one event, naming the row as its source");

		row.Refresh(events[0].Source);
		Expect(row.IsPinned, "the row reads the pin back after its own write");

		// The row's text box is ahead of the store: an echo of the row's own write must not overwrite it.
		row.Comment = "abcd";
		row.Refresh(events[0].Source);
		Expect(row.Comment == "abcd", "an echo of the row's own write does not overwrite newer text");

		annotations.SetComment(row.LineNumber, "from another view", source: null);
		row.Refresh(null);
		Expect(row.Comment == "from another view", "a change from elsewhere is read back");

		row.Comment = string.Empty;
		Expect(!annotations.IsPinned(row.LineNumber), "removing the comment unpins the record");

		annotations.SetPinned(5, true, null);
		annotations.SetPinned(50, true, null);
		events.Clear();
		Expect(annotations.UnpinAll() == 2 && events.Count == 1 && events[0].LineNumber == AnnotationChangedEventArgs.AllLines, "Unpin All raises one event");
	}

	private static void CheckRemapAtScale()
	{
		const int count = 100_000_000;
		var stopwatch = Stopwatch.StartNew();
		var all = SyntheticLog.Filter(FilterKind.AllRecords, count);
		var even = SyntheticLog.Filter(FilterKind.EverySecondRecord, count);
		var tenth = SyntheticLog.Filter(FilterKind.EveryTenthRecord, count);
		Console.WriteLine($"Filters over {count:N0} records: {stopwatch.ElapsedMilliseconds:N0} ms");

		var selection = new RangeSelection();
		stopwatch.Restart();
		selection.SelectAll(all.Length);
		Console.WriteLine($"Select all: {stopwatch.Elapsed.TotalMilliseconds:N3} ms");

		stopwatch.Restart();
		var remapped = RangeSelection.Remap(selection, all, even);
		Console.WriteLine($"Keep the selection, all to every 2nd: {stopwatch.ElapsedMilliseconds:N0} ms, {remapped.RangeCount:N0} range(s)");
		Expect(remapped.Count == even.Length, "all of every 2nd record stays selected");

		stopwatch.Restart();
		remapped = RangeSelection.Remap(remapped, even, tenth);
		Console.WriteLine($"Keep the selection, every 2nd to every 10th: {stopwatch.ElapsedMilliseconds:N0} ms, {remapped.RangeCount:N0} range(s)");
		Expect(remapped.Count == tenth.Length, "all of every 10th record stays selected");

		stopwatch.Restart();
		remapped = RangeSelection.Remap(remapped, tenth, all);
		Console.WriteLine($"Keep the selection, every 10th to all: {stopwatch.ElapsedMilliseconds:N0} ms, {remapped.RangeCount:N0} range(s)");
		Expect(remapped.Count == tenth.Length && remapped.RangeCount == tenth.Length, "the worst case: one range per selected record");

		var list = new VirtualRowList(new AnnotationStore(), _ => false);
		list.Reset(all);
		RowViewModel row = list.GetRow(77_777_777);
		const int lookups = 1_000_000;
		stopwatch.Restart();
		for (var i = 0; i < lookups; i++)
		{
			list.IndexOf(row);
		}

		Console.WriteLine($"IndexOf over {count:N0} rows: {stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / lookups:N0} ns per call");
	}

	private static void Expect(bool condition, string description)
	{
		if (!condition)
		{
			_failures++;
			Console.WriteLine("FAILED: " + description);
		}
	}
}
