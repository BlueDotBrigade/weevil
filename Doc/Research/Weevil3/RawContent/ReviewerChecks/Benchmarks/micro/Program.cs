using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

var path = args[0];
var unit = args[1] == "utf16" ? 2 : 1;
var bytes = File.ReadAllBytes(path);
var start = unit == 2 ? 2 : 0;
var offsets = new List<(int Offset, int Length)>();
var lineStart = start;
for (var i = start; i + unit <= bytes.Length; i += unit)
{
	var c = unit == 2 ? BitConverter.ToUInt16(bytes, i) : bytes[i];
	if (c == '\n')
	{
		var end = i - unit; // drop \r
		offsets.Add((lineStart, end - lineStart));
		lineStart = i + unit;
	}
}
Encoding enc = unit == 2 ? new UnicodeEncoding(false, false) : new UTF8Encoding(false);
var strings = offsets.Select(o => enc.GetString(bytes, o.Offset, o.Length)).ToArray();
Console.WriteLine($"records={offsets.Count}");
char[] buffer = new char[16];
const string needle = "zzzz-not-there";

double best1 = 1e9, best2 = 1e9, best3 = 1e9;
for (var round = 0; round < 9; round++)
{
	var sw = Stopwatch.StartNew();
	long sum = 0;
	foreach (var (o, l) in offsets)
	{
		var max = enc.GetMaxCharCount(l);
		if (buffer.Length < max) buffer = new char[max];
		sum += enc.GetChars(bytes, o, l, buffer, 0);
	}
	best1 = Math.Min(best1, sw.Elapsed.TotalMilliseconds);

	sw.Restart();
	foreach (var (o, l) in offsets)
	{
		ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes.AsSpan(o, l));
		if (unit == 2 && (l & 1) == 0 && chars.IndexOfAnyInRange('\uD800', '\uDFFF') < 0)
		{
			if (buffer.Length < chars.Length) buffer = new char[chars.Length];
			chars.CopyTo(buffer);
			sum += chars.Length;
		}
	}
	best2 = Math.Min(best2, sw.Elapsed.TotalMilliseconds);

	sw.Restart();
	foreach (var s in strings) sum += s.Length;
	best3 = Math.Min(best3, sw.Elapsed.TotalMilliseconds);
	GC.KeepAlive(sum);
}
Console.WriteLine($"min of 9: Encoding.GetChars={best1:F1}ms ({best1 * 1e6 / offsets.Count:F0} ns/record)  cast+surrogateCheck+copy={best2:F1}ms ({best2 * 1e6 / offsets.Count:F0} ns/record)  stringLength={best3:F1}ms");
