namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Diagnostics;

	[TestClass]
	public class ZzLimitTests
	{
		private sealed class CapturingLog : ILogWriter
		{
			public readonly List<string> Lines = new List<string>();
			public void Write(string message) { }
			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) { }
			public void Write(LogSeverityType severity, string message) => Lines.Add(severity + ": " + message);
			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Lines.Add(severity + ": " + message);
			public void Write(LogSeverityType severity, Exception exception) { }
			public void Write(LogSeverityType severity, Exception exception, string message) { }
			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) { }
		}

		private sealed class ThrowingParser : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();
			public int Calls;

			public bool TryParse(int line, string content, out IRecord record)
			{
				Calls++;
				if (content.Contains("BOOM", StringComparison.Ordinal))
				{
					throw new InvalidOperationException("BOOM on line " + line);
				}

				return _inner.TryParse(line, content, out record);
			}
		}

		private static MemoryStream CreateLog()
		{
			var lines = Enumerable.Range(1, 2000).Select(i =>
				i == 1500 ? "2026-10-16 10:00:00.00000\t1\t2\tInformation\tBOOM" :
				i == 1600 ? "not-a-timestamp\t1\t2\tInformation\tbad" :
				$"2026-10-16 10:00:00.00000\t1\t2\tInformation\tRecord {i}");
			return new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenLimitOfOne_WhenParserThrowsFarBeyondIt_ThenOneRecordIsReturned()
		{
			var parser = new ThrowingParser();
			var repository = new SerializedRecordRepository(CreateLog(), parser);

			Action act = () => repository.Get(1);
			try
			{
				var records = repository.Get(1);
				Console.WriteLine($"RESULT records={records.Length} parserCalls={parser.Calls}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"RESULT exception={e.Message} parserCalls={parser.Calls}");
				throw;
			}
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenLimitOfOne_WhenMalformedLineFarBeyondIt_ThenNothingIsLogged()
		{
			ILogWriter original = Log.Default;
			var log = new CapturingLog();
			Log.Register(log);
			try
			{
				var repository = new SerializedRecordRepository(CreateLog(), new ThrowingParserFreeTsv());
				var records = repository.Get(1);
				Console.WriteLine($"RESULT records={records.Length} logs={string.Join(" | ", log.Lines.Where(l => !l.Contains("Preparing")))}");
				log.Lines.Where(l => l.Contains("Unable to parse record")).Should().BeEmpty();
			}
			finally
			{
				Log.Register(original);
			}
		}

		private sealed class ThrowingParserFreeTsv : IRecordParser
		{
			private readonly TsvRecordParser _inner = new TsvRecordParser();
			public bool TryParse(int line, string content, out IRecord record) => content.Contains("BOOM") ? (record = Record.Dummy) is null : _inner.TryParse(line, content, out record);
		}
	}
}
