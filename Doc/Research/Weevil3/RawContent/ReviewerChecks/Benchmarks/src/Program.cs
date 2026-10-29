using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Text;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Data;

internal static class Program
{
	private static readonly string[] Words = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliet", "kilo", "lima", "mike", "november", "oscar", "papa", "quebec", "romeo", "sierra", "tango", "uniform", "victor", "whiskey", "xray", "yankee", "zulu"];

	public static int Main(string[] args)
	{
		switch (args[0])
		{
			case "gen": Generate(args[1], int.Parse(args[2]), args.Length > 3 ? args[3] : "utf8"); return 0;
			case "trunc": Trunc(args[1]); return 0;
			case "open": Open(args[1]); return 0;
			case "clear": Clear(args[1], int.Parse(args[2])); return 0;
			case "retain": { var e = OpenEngine(args[1], out _); Settled(); Walk.FindLargeRecordArrays(e, int.MaxValue); GC.KeepAlive(e); return 0; }
			case "graphx":
			{
				var e = OpenEngine(args[1], out _);
				var rx = new System.Text.RegularExpressions.Regex(@"\b(?:AM|PM)\b|\b(?:A\.?M\.?|P\.?M\.?)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
				var records = e.Records;
				var best = double.MaxValue; var bestOnce = double.MaxValue;
				for (var r = 0; r < 5; r++)
				{
					var sw = Stopwatch.StartNew();
					var any = records.Any(record => !string.IsNullOrWhiteSpace(record.Content) && rx.IsMatch(record.Content));
					best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
					sw.Restart();
					var any2 = records.Any(record => { var c = record.Content; return !string.IsNullOrWhiteSpace(c) && rx.IsMatch(c); });
					bestOnce = Math.Min(bestOnce, sw.Elapsed.TotalMilliseconds);
				}
				var bestBuffer = double.NaN;
#if BRANCH
				bestBuffer = double.MaxValue;
				for (var r = 0; r < 5; r++)
				{
					var sw = Stopwatch.StartNew();
					var buffer = new BlueDotBrigade.Weevil.Data.ContentBuffer();
					var any3 = records.Any(record => { var c = buffer.Read(record); return !c.IsWhiteSpace() && rx.IsMatch(c); });
					bestBuffer = Math.Min(bestBuffer, sw.Elapsed.TotalMilliseconds);
				}
#endif
				Console.WriteLine($"GetXAxisTimeFormat scan over {records.Length} records: twoReads={best:F0}ms oneRead={bestOnce:F0}ms contentBuffer={bestBuffer:F0}ms");
				return 0;
			}
			case "find": Find(args[1], int.Parse(args[2])); return 0;
			case "filter": FilterBench(args[1], args[2], int.Parse(args[3])); return 0;
		}
		return 1;
	}

	private static void Generate(string path, int megabytes, string encodingName)
	{
		var random = new Random(42);
		var time = new DateTime(2026, 10, 8, 10, 0, 0);
		var sb = new StringBuilder(256);
		long written = 0;
		long target = megabytes * 1_000_000L;
		var seq = 0;
		Encoding enc = encodingName == "utf16" ? new UnicodeEncoding(false, true) : new UTF8Encoding(false);
		using var w = new StreamWriter(path, false, enc, 1 << 20);
		while (written < target)
		{
			time = time.AddTicks(random.Next(0, 2000) * 100L);
			sb.Clear();
			sb.Append(time.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture)).Append('\t').Append(1000).Append('\t').Append(random.Next(2, 25)).Append('\t').Append(random.Next(100) < 90 ? "Information" : "Error").Append('\t');
			var n = random.Next(0, 18);
			for (var i = 0; i < n; i++) sb.Append(Words[random.Next(Words.Length)]).Append(' ');
			sb.Append("sequence=").Append(seq++).Append("\r\n");
			if (encodingName.StartsWith("ml"))
			{
				var lines = random.Next(3, 15);
				for (var l = 0; l < lines; l++) sb.Append("   at Company.Product.Module").Append(random.Next(100)).Append(".Method(String value, Int32 count) in C:\\src\\file.cs:line ").Append(random.Next(1000)).Append(encodingName == "mllf" ? "\n" : "\r\n");
			}
			w.Write(sb);
			written += sb.Length;
		}
	}

	private static IEngine OpenEngine(string path, out double seconds)
	{
		var sw = Stopwatch.StartNew();
		IEngine engine = Engine.UsingPath(path).Open();
		seconds = sw.Elapsed.TotalSeconds;
		return engine;
	}

	private static long Settled()
	{
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		return GC.GetTotalMemory(false);
	}

	private static void Trunc(string path)
	{
		IEngine engine = OpenEngine(path, out _);
		var callStack = new SimpleCallStackFormatter();
		var shortened = new ShortenedRecordFormatter(8 * 1024, 256);
		int shown = 0;
		foreach (IRecord record in engine.Records)
		{
			// Exactly ContentToTruncationIconConverter's logic: ContentConverter.Convert returns object.
			object convertedText = shortened.Format(callStack.Format(record));
			var icon = record.Content == convertedText ? string.Empty : " ... ";
			if (icon.Length > 0) shown++;
		}
		Console.WriteLine($"records={engine.Count} truncationIconShown={shown} type={engine.Records[0].GetType().Name}");
	}

	private static void Open(string path)
	{
		var before = Settled();
		using var p = Process.GetCurrentProcess();
		IEngine engine = OpenEngine(path, out var s);
		p.Refresh();
		var ws = p.WorkingSet64;
		var peak = p.PeakWorkingSet64;
		var after = Settled();
		p.Refresh();
		Console.WriteLine($"records={engine.Count} open={s:F2}s managedAfterGC={(after - before) / 1e6:F1}MB perRecord={(after - before) / (double)engine.Count:F1}B wsAfterOpen={ws / 1e6:F0}MB peakWS={peak / 1e6:F0}MB wsAfterGC={p.WorkingSet64 / 1e6:F0}MB gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)} gen2={GC.CollectionCount(2)}");
		GC.KeepAlive(engine);
	}

	private static void Clear(string path, int every)
	{
		var before = Settled();
		IEngine engine = OpenEngine(path, out _);
		var opened = Settled();
		var weak = DoClear(engine, every);
		var cleared = Settled();
		Console.WriteLine($"cleared record alive={weak.IsAlive}");
		Walk.Target = weak.Target;
		Walk.FindLargeRecordArrays(engine, 10000);
		Console.WriteLine($"records={engine.Records.Length} openedManaged={(opened - before) / 1e6:F1}MB afterClear={(cleared - before) / 1e6:F1}MB freed={(opened - cleared) * 100.0 / (opened - before):F1}%");
		GC.KeepAlive(engine);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static WeakReference DoClear(IEngine engine, int every)
	{
		var keep = new List<IRecord>();
		var all = engine.Records;
		if (every < 0) keep.Add(all[all.Length / 2]); else for (var i = 0; i < all.Length; i += every) keep.Add(all[i]);
		var weak = new WeakReference(all[all.Length - 1]);
		engine.Selector.Select(keep);
		engine.Clear(every < 0 ? ClearOperation.AfterSelected : ClearOperation.Unselected);
		return weak;
	}

	private static void Find(string path, int repeat)
	{
		IEngine engine = OpenEngine(path, out _);
		var times = new List<double>();
		for (var r = 0; r < repeat; r++)
		{
			engine.Navigate.GoTo(engine.Records[0].LineNumber, BlueDotBrigade.Weevil.Navigation.RecordSearchType.ExactMatch);
			var sw = Stopwatch.StartNew();
			try { engine.Navigate.NextContent("zzzz-not-there", Environment.GetEnvironmentVariable("CASE") == "1"); } catch (Exception e) { if (r == 0) Console.WriteLine(e.GetType().Name); }
			times.Add(sw.Elapsed.TotalMilliseconds);
		}
		times.Sort();
		Console.WriteLine($"records={engine.Count} find min={times[0]:F0}ms median={times[times.Count / 2]:F0}ms allocated={GC.GetTotalAllocatedBytes() / 1e6:F0}MB");
	}

	private static void FilterBench(string path, string text, int repeat)
	{
		IEngine engine = OpenEngine(path, out _);
		var times = new List<double>();
		for (var r = 0; r < repeat; r++)
		{
			var sw = Stopwatch.StartNew();
			engine.Filter.Apply(BlueDotBrigade.Weevil.Filter.FilterType.PlainText, new BlueDotBrigade.Weevil.Filter.FilterCriteria(text));
			times.Add(sw.Elapsed.TotalMilliseconds);
		}
		times.Sort();
		Console.WriteLine($"records={engine.Count} results={engine.Filter.Results.Length} filter min={times[0]:F0}ms median={times[times.Count / 2]:F0}ms");
	}
}
