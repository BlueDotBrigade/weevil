namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class ZzFuzzReaderTests
	{
		private sealed class ChoppyStream : MemoryStream
		{
			private readonly Random _rnd;
			public ChoppyStream(byte[] b, int seed) : base(b) { _rnd = new Random(seed); }
			public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, _rnd.Next(1, 4)));
			public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(buffer.Length, _rnd.Next(1, 4))));
		}

		private static readonly byte[] Alphabet = { 0x0D, 0x0A, 0x00, 0x41, 0xD8, 0xDC, 0xFF, 0xFE, 0xEF, 0xBB, 0xBF, 0xE2, 0x82, 0xAC, 0xF0, 0x9F, 0xC0, 0x80, 0x11, 0x20 };

		private static List<string> ReadOld(byte[] bytes)
		{
			var lines = new List<string>();
			using var reader = new StreamReader(new MemoryStream(bytes));
			string line;
			while ((line = reader.ReadLine()) != null) lines.Add(line);
			return lines;
		}

		private static List<string> ReadNew(Stream stream, int chunk)
		{
			var lines = new List<string>();
			var reader = new LogLineReader(stream, 0, chunk);
			while (reader.TryReadLine(out var offset, out var length, out var text))
			{
				// The bytes at the offset must decode to the same text.
				lines.Add(text);
			}
			return lines;
		}

		[TestMethod]
		public void Fuzz()
		{
			var rnd = new Random(12345);
			var prefixes = new[] { Array.Empty<byte>(), new byte[] { 0xEF, 0xBB, 0xBF }, new byte[] { 0xFF, 0xFE }, new byte[] { 0xFE, 0xFF }, new byte[] { 0xFF, 0xFE, 0, 0 }, new byte[] { 0, 0, 0xFE, 0xFF } };
			var failures = 0;
			for (var i = 0; i < 200000; i++)
			{
				var prefix = prefixes[rnd.Next(prefixes.Length)];
				var len = rnd.Next(0, 40);
				var body = new byte[len];
				for (var j = 0; j < len; j++) body[j] = Alphabet[rnd.Next(Alphabet.Length)];
				var bytes = prefix.Concat(body).ToArray();
				var expected = ReadOld(bytes);
				var actual1 = ReadNew(new MemoryStream(bytes), 1 + rnd.Next(64));
				var actual2 = ReadNew(new ChoppyStream(bytes, i), 1 + rnd.Next(64));
				if (!expected.SequenceEqual(actual1) || !expected.SequenceEqual(actual2))
				{
					if (failures++ < 10)
					{
						Console.WriteLine($"MISMATCH {Convert.ToHexString(bytes)} old=[{string.Join("|", expected.Select(Esc))}] new=[{string.Join("|", actual1.Select(Esc))}] choppy=[{string.Join("|", actual2.Select(Esc))}]");
					}
				}
			}
			Console.WriteLine("FUZZ failures=" + failures);
		}

		private static string Esc(string s) => string.Concat(s.Select(c => c < 0x20 || c > 0x7e ? $"\\u{(int)c:X4}" : c.ToString()));
	}
}
