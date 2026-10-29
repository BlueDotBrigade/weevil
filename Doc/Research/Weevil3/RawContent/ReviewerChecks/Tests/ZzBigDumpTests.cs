namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	[TestClass]
	public class ZzBigDumpTests
	{
		private sealed class TrimSomeParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();
			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out IRecord r))
				{
					// Changes the text of some records only, so a chunk mixes strings and bytes.
					record = content.EndsWith(" ", StringComparison.Ordinal)
						? new Record(r.LineNumber, r.CreatedAt, r.Severity, r.Content.TrimEnd(), false, r.WasGeneratedByUi)
						: r;
					return true;
				}
				record = Record.Dummy;
				return false;
			}
		}

		private sealed class NoSilentLog : ILogWriter
		{
			public int Count;
			public void Write(string message) { }
			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) { }
			public void Write(LogSeverityType severity, string message) => Count++;
			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Count++;
			public void Write(LogSeverityType severity, Exception exception) { }
			public void Write(LogSeverityType severity, Exception exception, string message) { }
			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) { }
		}

		private static string BuildText(int seed)
		{
			var rnd = new Random(seed);
			var sb = new StringBuilder();
			var breaks = new[] { "\n", "\r\n", "\r" };
			var i = 0;
			while (sb.Length < 37_000_000)
			{
				i++;
				var kind = rnd.Next(100);
				if (kind < 70)
				{
					sb.Append($"2026-10-16 10:00:{i % 60:00}.00000\t1\t{rnd.Next(1, 3)}\tError\tRecord {i} {new string('x', rnd.Next(0, 300))}{(rnd.Next(5) == 0 ? "  " : "")}");
				}
				else if (kind < 95)
				{
					sb.Append($"  continuation {i} é中\U0001F600 {new string('y', rnd.Next(0, 200))}");
				}
				else if (kind < 99)
				{
					sb.Append("");
				}
				else
				{
					sb.Append(new string('z', rnd.Next(60_000, 140_000)));
				}
				sb.Append(breaks[rnd.Next(3)]);
			}
			return sb.ToString();
		}

		[TestMethod]
		[DoNotParallelize]
		public void Dump()
		{
			var output = Environment.GetEnvironmentVariable("DIFF_OUT");
			if (string.IsNullOrEmpty(output)) Assert.Inconclusive();

			ILogWriter original = Log.Default;
			using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
			var text = BuildText(7);
			var logs = new (string, byte[])[]
			{
				("utf8", new UTF8Encoding(false).GetBytes(text)),
				("utf16le-bom", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray()),
			};

			foreach ((string logName, byte[] bytes) in logs)
			{
				var stream0 = new MemoryStream(bytes);
				LineIndex index = LineIndex.Build(stream0);
				var starts = new[] { 1, 2, index.LineCount / 3, index.LineCount / 2 + 1, index.LineCount - 1 };

				foreach ((string parserName, Func<IRecordParser> create) in new (string, Func<IRecordParser>)[] { ("tsv", () => new TsvRecordParser()), ("trimsome", () => new TrimSomeParser()), ("default", () => new DefaultRecordParser()) })
				{
					foreach (var start in starts)
					{
						var log = new NoSilentLog();
						Log.Register(log);
						try
						{
							var stream = new MemoryStream(bytes);
							SerializedRecordRepository repository = start == 1
								? new SerializedRecordRepository(stream, create())
								: new SerializedRecordRepository(stream, create(), index.GetByteOffset(stream, start), start, true);
							var records = repository.Get(int.MaxValue);
							using var sha = SHA256.Create();
							var count = 0;
							foreach (IRecord r in records)
							{
								var line = $"{r.LineNumber} {r.CreatedAt.Ticks} {r.Severity} {r.IsMultiLine} {r.WasGeneratedByUi} {r.HasContent} {r.Content}\u0001";
								var b = Encoding.UTF8.GetBytes(line);
								sha.TransformBlock(b, 0, b.Length, null, 0);
								count++;
							}
							sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
							writer.WriteLine($"{logName} {parserName} start={start} records={count} logs={log.Count} sha={Convert.ToHexString(sha.Hash)}");
						}
						finally
						{
							Log.Register(original);
						}
					}
				}
			}
		}
	}
}
