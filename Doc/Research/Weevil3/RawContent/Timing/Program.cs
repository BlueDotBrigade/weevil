using System.Collections.Concurrent;
using System.Diagnostics;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;
using BlueDotBrigade.Weevil.Navigation;

// Opens each log, then times each filter and Find: the median of 3 runs, after one warm-up run.
var label = args[0];
var cases = new (string Name, FilterType Type, string Include, bool CaseSensitive)[]
{
	("PlainText NoSuchText", FilterType.PlainText, "NoSuchText", true),
	("PlainText Gyroscope", FilterType.PlainText, "Gyroscope", true),
	("PlainText gyroscope (ignore case)", FilterType.PlainText, "gyroscope", false),
	("Regex Gyroscope", FilterType.RegularExpression, "Gyroscope", true),
	("Regex Security", FilterType.RegularExpression, "Security", true),
	("Regex NoSuchText\\d+", FilterType.RegularExpression, @"NoSuchText\d+", true),
	("Regex Pitch=\\d+\\.\\d", FilterType.RegularExpression, @"Pitch=\d+\.\d", true),
	("Moniker @Severity=Error", FilterType.RegularExpression, "@Severity=Error", true),
	("No filter", FilterType.PlainText, "", true),
};
foreach (var file in args.Skip(1))
{
	var open = Stopwatch.StartNew();
	IEngine engine = Engine.UsingPath(file).Open();
	open.Stop();
	Console.WriteLine($"{label}\t{Path.GetFileName(file)}\tOpen\t{open.Elapsed.TotalSeconds:F2}\t{engine.Count}");
	foreach (var c in cases)
	{
		var times = new List<double>();
		var count = 0;
		for (var i = 0; i < 4; i++)
		{
			var configuration = new ConcurrentDictionary<string, object> { ["IsCaseSensitive"] = c.CaseSensitive };
			engine.Filter.Apply(c.Type, new FilterCriteria(c.Include, string.Empty, configuration));
			if (i > 0) times.Add(engine.Filter.FilterExecutionTime.TotalSeconds);
			count = engine.Filter.Results.Length;
		}
		times.Sort();
		Console.WriteLine($"{label}\t{Path.GetFileName(file)}\t{c.Name}\t{times[1]:F3}\t{count}");
	}
	engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);
	foreach (var (name, regex) in new[] { ("Find NoSuchText", false), ("Find NoSuchText\\d+ (regex)", true) })
	{
		var times = new List<double>();
		for (var i = 0; i < 4; i++)
		{
			engine.Navigate.GoTo(engine.Records[0].LineNumber, RecordSearchType.ExactMatch);
			var sw = Stopwatch.StartNew();
			try { engine.Navigate.NextContent(regex ? @"NoSuchText\d+" : "NoSuchText", isCaseSensitive: true, useRegex: regex); } catch (Exception) { }
			sw.Stop();
			if (i > 0) times.Add(sw.Elapsed.TotalSeconds);
		}
		times.Sort();
		Console.WriteLine($"{label}\t{Path.GetFileName(file)}\t{name}\t{times[1]:F3}\t");
	}
}
