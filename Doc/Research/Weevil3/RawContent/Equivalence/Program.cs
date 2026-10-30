using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Data;

// Opens each log, from the top and from a few start lines, and writes every record's line number, facts and content.
var outPath = args[0];
var root = args[1];
var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
	.Where(f => !f.EndsWith(".idx") && !f.EndsWith(".xml") && !f.EndsWith(".xml~"))
	.OrderBy(f => f, StringComparer.Ordinal)
	.ToArray();
var starts = args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 0, 2, 7, 100 };

using var writer = new StreamWriter(outPath, false, new UTF8Encoding(false));
foreach (var file in files)
{
	var isSmall = new FileInfo(file).Length < 5_000_000;
	foreach (var start in starts)
	{
		writer.WriteLine($"FILE {Path.GetRelativePath(root, file)} START {start}");
		try
		{
			var stopwatch = Stopwatch.StartNew();
			IEngine engine = start == 0 ? Engine.UsingPath(file).Open() : Engine.UsingPath(file, start).Open();
			stopwatch.Stop();
			using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#if NEWBUILD
			var buffer = new ContentBuffer();
			var mismatches = 0;
#endif
			foreach (IRecord r in engine.Records)
			{
				var line = $"{r.LineNumber}\t{r.Severity}\t{r.CreatedAt:o}\t{r.IsMultiLine}\t{r.WasGeneratedByUi}\t{r.HasContent}\t{JsonSerializer.Serialize(r.Content)}";
				hash.AppendData(Encoding.UTF8.GetBytes(line));
				if (isSmall)
				{
					writer.WriteLine(line);
				}
#if NEWBUILD
				if (!buffer.Read(r).SequenceEqual(r.Content.AsSpan()))
				{
					mismatches++;
				}
#endif
			}
			writer.WriteLine($"COUNT {engine.Count} HASH {Convert.ToHexString(hash.GetHashAndReset())}");
#if NEWBUILD
			Console.Error.WriteLine($"{Path.GetFileName(file)} start={start} records={engine.Count} open={stopwatch.Elapsed.TotalSeconds:F2}s bufferMismatches={mismatches}");
#else
			Console.Error.WriteLine($"{Path.GetFileName(file)} start={start} records={engine.Count} open={stopwatch.Elapsed.TotalSeconds:F2}s");
#endif
		}
		catch (Exception e)
		{
			writer.WriteLine($"ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
		}
	}
}
