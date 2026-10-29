namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class ZzNullTests
	{
		// Reports success with no record for a blank line.
		private sealed class TrueWithNullForBlankParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();
			public bool TryParse(int line, string content, out IRecord record)
			{
				if (content.Length == 0) { record = null; return true; }
				return _inner.TryParse(line, content, out record);
			}
		}

		[TestMethod]
		public void TrueWithNull()
		{
			var text = "\n2026-10-16 10:00:00.00000\t1\t2\tInformation\tA\n\n2026-10-16 10:00:01.00000\t1\t2\tInformation\tB\n";
			try
			{
				var records = new SerializedRecordRepository(new MemoryStream(Encoding.UTF8.GetBytes(text)), new TrueWithNullForBlankParser()).Get(int.MaxValue);
				Console.WriteLine("RESULT " + string.Join(" | ", records.Select(r => $"{r.LineNumber}:{r.Content}")));
			}
			catch (Exception e)
			{
				Console.WriteLine("RESULT exception " + e.GetType().Name);
			}
		}
	}
}
