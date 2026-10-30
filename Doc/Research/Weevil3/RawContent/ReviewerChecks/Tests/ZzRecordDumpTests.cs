namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	// Throwaway differential harness: dumps every record the repository returns, plus the log messages, for many cases.
	[TestClass]
	public class ZzRecordDumpTests
	{
		private sealed class CapturingLog : ILogWriter
		{
			public readonly List<string> Lines = new List<string>();
			public void Write(string message) => Lines.Add("?: " + message);
			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Lines.Add("?: " + message);
			public void Write(LogSeverityType severity, string message) => Add(severity, message);
			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Add(severity, message);
			public void Write(LogSeverityType severity, Exception exception) => Add(severity, exception.GetType().Name);
			public void Write(LogSeverityType severity, Exception exception, string message) => Add(severity, message);
			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Add(severity, message);

			private void Add(LogSeverityType severity, string message)
			{
				if (message.StartsWith("Preparing to read", StringComparison.Ordinal))
				{
					return;
				}

				Lines.Add(severity + ": " + message);
			}
		}

		private sealed class MultiLineFlagParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out IRecord r))
				{
					record = new Record(r.LineNumber, r.CreatedAt, r.Severity, r.Content, isMultiLine: true, r.WasGeneratedByUi);
					return true;
				}

				record = Record.Dummy;
				return false;
			}
		}

		private sealed class TrimParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out IRecord r))
				{
					record = new Record(r.LineNumber, r.CreatedAt, r.Severity, r.Content.Trim(), false, r.WasGeneratedByUi);
					return true;
				}

				record = Record.Dummy;
				return false;
			}
		}

		private sealed class CustomRecord : IRecord
		{
			public string Content { get; init; }
			public bool HasCreationTime => true;
			public DateTime CreatedAt { get; init; }
			public bool HasContent => !string.IsNullOrWhiteSpace(Content);
			public int LineNumber { get; init; }
			public SeverityType Severity { get; init; }
			public bool IsMultiLine { get; init; }
			public bool WasGeneratedByUi { get; init; }
		}

		private sealed class CustomTypeParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out IRecord r))
				{
					record = new CustomRecord { Content = r.Content, CreatedAt = r.CreatedAt, LineNumber = r.LineNumber, Severity = r.Severity, IsMultiLine = true, WasGeneratedByUi = r.WasGeneratedByUi };
					return true;
				}

				record = Record.Dummy;
				return false;
			}
		}

		private sealed class ShiftParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out IRecord r))
				{
					record = new Record(r.LineNumber + 1000, r.CreatedAt, r.Severity, r.Content, false, !r.WasGeneratedByUi);
					return true;
				}

				record = Record.Dummy;
				return false;
			}
		}

		private sealed class FalseButNotDummyParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (_inner.TryParse(line, content, out record))
				{
					return true;
				}

				record = new Record(line, Record.CreationTimeUnknown, SeverityType.Verbose, "rejected:" + content);
				return false;
			}
		}

		private sealed class ThrowingParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				if (content.Contains("BOOM", StringComparison.Ordinal))
				{
					throw new InvalidOperationException("BOOM on line " + line);
				}

				return _inner.TryParse(line, content, out record);
			}
		}

		private sealed class StatefulParser : IRecordParser
		{
			// Counts calls: a parser whose answer depends on how many lines it has seen.
			private int _calls;
			private readonly TsvRecordParser _inner = new TsvRecordParser();

			public bool TryParse(int line, string content, out IRecord record)
			{
				_calls++;
				var ok = _inner.TryParse(line, content, out IRecord r);
				record = ok ? new Record(r.LineNumber, r.CreatedAt, r.Severity, r.Content + "#" + _calls, false, false) : Record.Dummy;
				return ok;
			}
		}

		private static string Rec(int i, string text) => $"2026-10-16 10:00:{i % 60:00}.00000\t1\t{(i % 3 == 0 ? 1 : 2)}\t{(i % 2 == 0 ? "Error" : "Information")}\t{text}";

		private static IEnumerable<(string Name, string Text)> Texts()
		{
			yield return ("tsv-mixed", string.Join("", new[]
			{
				"junk 1\n",
				"junk 2\r\n",
				Rec(3, "first") + "\r\n",
				"cont a\n",
				"\n",
				"  \r",
				"cont b\r\n",
				Rec(8, "second BOOMx") + "\n",
				Rec(9, "third") + "\r",
				"\r\n",
				Rec(11, "fourth   ") + "\r\n",
				"   \t  \r\n",
				Rec(13, "fifth") + "\n",
				"cont é中\U0001F600\r\r\n",
				Rec(15, "sixth"),
			}));

			yield return ("tsv-trailing-blank", Rec(1, "only") + "\r\n\r\n\n");
			yield return ("tsv-cr-only", Rec(1, "a") + "\r" + "x\r" + Rec(3, "b") + "\r\r");
			yield return ("whitespace-records", "   \n\t\n" + Rec(3, " ") + "\n \n");
			yield return ("empty", "");
			yield return ("one-break", "\n");
			yield return ("no-records", "junk\njunk\r\njunk");
		}

		private static IEnumerable<(string Name, byte[] Bytes)> Logs()
		{
			foreach ((string name, string text) in Texts())
			{
				yield return (name + "-utf8", new UTF8Encoding(false).GetBytes(text));
				yield return (name + "-utf8bom", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray());
				yield return (name + "-utf16le", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray());
				yield return (name + "-utf16be", Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(text)).ToArray());
				yield return (name + "-utf32le", Encoding.UTF32.GetPreamble().Concat(Encoding.UTF32.GetBytes(text)).ToArray());
				var be32 = new UTF32Encoding(true, true);
				yield return (name + "-utf32be", be32.GetPreamble().Concat(be32.GetBytes(text)).ToArray());
				// UTF-16 without a BOM is read as UTF-8 by both.
				yield return (name + "-utf16le-nobom", Encoding.Unicode.GetBytes(text));
			}

			// Invalid bytes next to line breaks, in a multi-line record.
			var u8 = new UTF8Encoding(false);
			yield return ("invalid-utf8", u8.GetBytes(Rec(1, "x") + "\n").Concat(new byte[] { 0xE2, 0x82, 0x0D, 0x0A, 0x82, 0xAC, 0x0A, 0xF0, 0x9F, 0x0D, 0xC0, 0x8A, 0x0A, 0xED, 0xA0, 0x80, 0x0A }).Concat(u8.GetBytes(Rec(9, "y"))).Concat(new byte[] { 0xF0 }).ToArray());
			// UTF-16 LE: high surrogate before a line break, low surrogate after one.
			var u16 = Encoding.Unicode;
			yield return ("invalid-utf16", u16.GetPreamble().Concat(u16.GetBytes(Rec(1, "x") + "\n")).Concat(new byte[] { 0x00, 0xD8, 0x0D, 0x00, 0x00, 0xDC, 0x41, 0x00, 0x0A, 0x00, 0x00, 0xD8 }).Concat(new byte[] { 0x0D, 0x00, 0x0A, 0x00 }).Concat(u16.GetBytes(Rec(4, "y") + "\r")).Concat(new byte[] { 0x0A }).ToArray());
			// UTF-32 LE: code units above U+10FFFF and surrogate code points.
			var u32 = Encoding.UTF32;
			yield return ("invalid-utf32", u32.GetPreamble().Concat(u32.GetBytes(Rec(1, "x") + "\n")).Concat(new byte[] { 0x00, 0x00, 0x11, 0x00, 0x0A, 0x00, 0x01, 0x00, 0x00, 0xD8, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00 }).Concat(u32.GetBytes(Rec(4, "y"))).Concat(new byte[] { 0x0A, 0x00 }).ToArray());
			// A BOM followed by another BOM-like sequence.
			yield return ("bom-then-fffe", new byte[] { 0xEF, 0xBB, 0xBF, 0xFF, 0xFE }.Concat(u8.GetBytes("\n" + Rec(2, "z"))).ToArray());
			yield return ("fffe-short", new byte[] { 0xFF, 0xFE, 0x41 });
			yield return ("fffe0000-utf32", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00 });
			yield return ("partial-bom", new byte[] { 0xEF, 0xBB });
			yield return ("bom-only", new byte[] { 0xEF, 0xBB, 0xBF });

			// Long lines that cross the reader's 64 KB buffer, with \r\n split across reads.
			var sb = new StringBuilder();
			for (var i = 1; i <= 40; i++)
			{
				sb.Append(Rec(i, new string((char)('a' + i % 26), 65536 - 40 + i))).Append(i % 3 == 0 ? "\r\n" : i % 3 == 1 ? "\r" : "\n");
				if (i % 4 == 0)
				{
					sb.Append(new string('c', 65500 + i)).Append("\r\n");
				}
			}

			yield return ("long-lines-utf8", u8.GetBytes(sb.ToString()));
			yield return ("long-lines-utf16be", Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(sb.ToString())).ToArray());
		}

		private static IEnumerable<(string Name, Func<IRecordParser> Create)> Parsers()
		{
			yield return ("tsv", () => new TsvRecordParser());
			yield return ("default", () => new DefaultRecordParser());
			yield return ("multilineflag", () => new MultiLineFlagParser());
			yield return ("trim", () => new TrimParser());
			yield return ("customtype", () => new CustomTypeParser());
			yield return ("shift", () => new ShiftParser());
			yield return ("falsebutnotdummy", () => new FalseButNotDummyParser());
			yield return ("throwing", () => new ThrowingParser());
			yield return ("stateful", () => new StatefulParser());
		}

		private static string Escape(string s)
		{
			var b = new StringBuilder();
			foreach (var c in s)
			{
				if (c < 0x20 || c >= 0x7F)
				{
					b.Append($"\\u{(int)c:X4}");
				}
				else
				{
					b.Append(c);
				}
			}

			return b.ToString();
		}

		[TestMethod]
		[DoNotParallelize]
		public void Dump()
		{
			var output = Environment.GetEnvironmentVariable("DIFF_OUT");
			if (string.IsNullOrEmpty(output))
			{
				Assert.Inconclusive("DIFF_OUT not set");
			}

			ILogWriter original = Log.Default;
			var writer = new StreamWriter(output, false, new UTF8Encoding(false));

			try
			{
				foreach ((string logName, byte[] bytes) in Logs())
				{
					foreach ((string parserName, Func<IRecordParser> create) in Parsers())
					{
						foreach (var startLine in new[] { 1, 2, 4, 7 })
						{
							foreach ((string rangeName, Range range) in new[] { ("all", Range.All), ("3-6", new Range(3, 6)) })
							{
								foreach (var limit in new[] { int.MaxValue, 1, 2 })
								{
									foreach (var maxSearch in new[] { MultilineRecordParser.MaximumLinesToSearch, 1, 2 })
									{
										if (startLine > 1 && maxSearch != MultilineRecordParser.MaximumLinesToSearch)
										{
											continue;
										}

										writer.WriteLine($"### {logName} parser={parserName} start={startLine} range={rangeName} limit={limit} search={maxSearch}");
										var log = new CapturingLog();
										Log.Register(log);

										try
										{
											var stream = new MemoryStream(bytes);
											SerializedRecordRepository repository;

											if (startLine == 1)
											{
												repository = new SerializedRecordRepository(stream, create(), maxSearch);
											}
											else
											{
												LineIndex index = LineIndex.Build(stream);
												if (startLine > index.LineCount)
												{
													writer.WriteLine("(skipped: beyond line count)");
													continue;
												}

												var offset = index.GetByteOffset(stream, startLine);
												repository = new SerializedRecordRepository(stream, create(), offset, startLine, true);
											}

											var records = repository.Get(range, limit);

											foreach (IRecord r in records)
											{
												writer.WriteLine($"{r.LineNumber} {r.CreatedAt.Ticks} {r.Severity} ML={r.IsMultiLine} UI={r.WasGeneratedByUi} HC={r.HasContent} T={(r is Record ? "Record" : r.GetType().Name)} [{Escape(r.Content)}]");
											}
										}
										catch (Exception e)
										{
											writer.WriteLine("EXCEPTION " + e.GetType().Name + ": " + e.Message);
										}
										finally
										{
											Log.Register(original);
										}

										foreach (var line in log.Lines)
										{
											writer.WriteLine("LOG " + line);
										}
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				writer.Dispose();
				Log.Register(original);
			}
		}
	}
}
