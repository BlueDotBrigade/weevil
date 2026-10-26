namespace BlueDotBrigade.Weevil
{
	using System.Globalization;
	using System.Text;

	/// <summary>
	/// Writes a synthetic Weevil TSV log for the performance reference conditions: records average 131 bytes,
	/// as in the #978 test log.
	/// </summary>
	/// <remarks>
	/// The same records as the memory harness writes (Doc/Research/Weevil3/MemoryHarness), so a test's figures compare with the harness's.
	/// </remarks>
	internal static class ReferenceLog
	{
		private const int AverageRecordBytes = 131;

		// Bytes in a record besides its words: timestamp, IDs, severity, tabs, "sequence=N" and the line break.
		private const double FixedRecordBytes = 62;

		private static readonly string[] Words =
		{
			"request", "accepted", "queue", "worker", "completed", "timeout", "retry", "connection",
			"sensor", "value", "updated", "cache", "miss", "session", "opened", "closed", "frame", "rendered",
		};

		public static string Create(string folder, int recordCount)
		{
			var path = Path.Combine(folder, $"ReferenceLog.{recordCount}.log");

			// Word counts are drawn evenly from 0 to twice the average, to vary record lengths.
			var averageWordBytes = Words.Average(word => word.Length + 1);
			var maxWords = (int)System.Math.Round((AverageRecordBytes - FixedRecordBytes) / averageWordBytes * 2);

			var random = new Random(42);
			var time = new DateTime(2026, 10, 8, 10, 0, 0);
			var line = new StringBuilder(256);

			using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

			for (var sequence = 0; sequence < recordCount; sequence++)
			{
				time = time.AddTicks(random.Next(0, 2_000) * 100L);
				var threadId = random.Next(5) == 0 ? 1 : random.Next(2, 25);

				line.Clear();
				line.Append(time.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture)).Append('\t')
					.Append(1000).Append('\t')
					.Append(threadId).Append('\t')
					.Append(random.Next(100) < 70 ? "Information" : "Debug").Append('\t');

				var wordCount = random.Next(0, maxWords + 1);
				for (var word = 0; word < wordCount; word++)
				{
					line.Append(Words[random.Next(Words.Length)]).Append(' ');
				}

				line.Append("sequence=").Append(sequence).Append("\r\n");
				writer.Write(line);
			}

			return path;
		}
	}
}
