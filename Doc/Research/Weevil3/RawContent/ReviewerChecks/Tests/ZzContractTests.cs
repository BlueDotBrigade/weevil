namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class ZzContractTests
	{
		// A parser that reports success for every line, with Record.Dummy for a line that does not start a record.
		private sealed class TrueWithDummyParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();
			public bool TryParse(int line, string content, out IRecord record)
			{
				_inner.TryParse(line, content, out record);
				return record != null;
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

		private static MemoryStream Log(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

		[TestMethod]
		public void TrueWithDummy()
		{
			var text = "header\n2026-10-16 10:00:00.00000\t1\t2\tInformation\tA\ncontinued\n2026-10-16 10:00:01.00000\t1\t2\tInformation\tB\n";
			var records = new SerializedRecordRepository(Log(text), new TrueWithDummyParser()).Get(int.MaxValue);
			Console.WriteLine("RESULT " + string.Join(" | ", records.Select(r => $"{r.LineNumber}:{r.IsMultiLine}:{r.Content.Replace("\r\n", "\\r\\n")}")));
			var records2 = new SerializedRecordRepository(Log(text.Substring(7)), new TrueWithDummyParser()).Get(int.MaxValue);
			Console.WriteLine("RESULT2 " + string.Join(" | ", records2.Select(r => $"{r.LineNumber}:{r.IsMultiLine}:{r.Content.Replace("\r\n", "\\r\\n")}")));
		}

		[TestMethod]
		public void MultiLineFlag()
		{
			var text = "2026-10-16 10:00:00.00000\t1\t2\tInformation\tA\n2026-10-16 10:00:01.00000\t1\t2\tInformation\tB\n";
			var records = new SerializedRecordRepository(Log(text), new MultiLineFlagParser()).Get(int.MaxValue);
			Console.WriteLine("RESULT " + string.Join(" | ", records.Select(r => $"{r.LineNumber}:IsMultiLine={r.IsMultiLine}")));
		}
	}
}
