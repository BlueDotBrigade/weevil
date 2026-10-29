namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;

	[TestClass]
	public class ZzFuzzRecordTests
	{
		private sealed class StartsWithAParser : IRecordParser
		{
			public bool TryParse(int line, string content, out IRecord record)
			{
				if (content.StartsWith('A'))
				{
					record = new Record(line, DateTime.MaxValue, SeverityType.Information, content);
					return true;
				}
				record = Record.Dummy;
				return false;
			}
		}

		private static readonly byte[] Alphabet = { 0x0D, 0x0A, 0x00, 0x41, 0x41, 0x41, 0x20, 0xD8, 0xDC, 0xFF, 0xFE, 0xEF, 0xBB, 0xBF, 0xE2, 0x82, 0xAC, 0xF0, 0x9F, 0xC0, 0x80, 0x11 };

		private static List<string> Expected(byte[] bytes)
		{
			var lines = new List<string>();
			using (var reader = new StreamReader(new MemoryStream(bytes)))
			{
				string line;
				while ((line = reader.ReadLine()) != null) lines.Add(line);
			}

			var result = new List<string>();
			var i = 0;
			while (i < lines.Count && !lines[i].StartsWith('A')) i++;
			while (i < lines.Count)
			{
				var start = i;
				var parts = new List<string> { lines[i] };
				i++;
				while (i < lines.Count && !lines[i].StartsWith('A')) { parts.Add(lines[i]); i++; }
				result.Add($"{start + 1}:{parts.Count > 1}:{!string.IsNullOrWhiteSpace(string.Join("\r\n", parts))}:{string.Join("\r\n", parts)}");
			}
			return result;
		}

		[TestMethod]
		public void Fuzz()
		{
			var rnd = new Random(777);
			var prefixes = new[] { Array.Empty<byte>(), new byte[] { 0xEF, 0xBB, 0xBF }, new byte[] { 0xFF, 0xFE }, new byte[] { 0xFE, 0xFF }, new byte[] { 0xFF, 0xFE, 0, 0 }, new byte[] { 0, 0, 0xFE, 0xFF } };
			var failures = 0;
			var buffer = new ContentBuffer();
			for (var n = 0; n < 200000; n++)
			{
				var prefix = prefixes[rnd.Next(prefixes.Length)];
				var len = rnd.Next(0, 60);
				var body = new byte[len];
				for (var j = 0; j < len; j++) body[j] = Alphabet[rnd.Next(Alphabet.Length)];
				var bytes = prefix.Concat(body).ToArray();
				var expected = Expected(bytes);

				var parser = new MultilineRecordParser(new LogLineReader(new MemoryStream(bytes), 0, 1 + rnd.Next(40)), new StartsWithAParser(), 256, 1, false);
				var actual = new List<string>();
				var viaBuffer = true;
				for (IRecord r = parser.GetNext(); Record.IsGenuine(r); r = parser.GetNext())
				{
					actual.Add($"{r.LineNumber}:{r.IsMultiLine}:{r.HasContent}:{r.Content}");
					viaBuffer &= buffer.Read(r).ToString() == r.Content;
				}

				if (!expected.SequenceEqual(actual) || !viaBuffer)
				{
					if (failures++ < 10)
					{
						Console.WriteLine($"MISMATCH {Convert.ToHexString(bytes)} buffer={viaBuffer}\n  old=[{string.Join(" || ", expected.Select(Esc))}]\n  new=[{string.Join(" || ", actual.Select(Esc))}]");
					}
				}
			}
			Console.WriteLine("FUZZ failures=" + failures);
		}

		private static string Esc(string s) => string.Concat(s.Select(c => c < 0x20 || c > 0x7e ? $"\\u{(int)c:X4}" : c.ToString()));
	}
}
