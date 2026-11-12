namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Data;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	/// <summary>
	/// Reading a screen of rows as the grid does: 50 rows, each read 5 times through <see cref="IRecord.Content"/>, as the row converters do (#1028).
	/// </summary>
	/// <remarks>
	/// On disk, each read opens the log, reads and checks the 64 KB blocks that hold the record, and closes the log.
	/// The #1028 plan keeps the content of the rows on screen in the row only if a screen takes more than 50 ms from a local SSD.
	/// After the first iteration the log is in the operating system's cache, so this measures the cost of the reads themselves,
	/// not the drive: on a drive, add its latency for each 64 KB block, once per row.
	/// </remarks>
	[MemoryDiagnoser]
	public class ScreenRowsBenchmarks
	{
		private const int Records = 1_000_000;
		private const int RowsOnScreen = 50;
		private const int ReadsPerRow = 5;

		private IEngine _engine = null!;
		private int _firstRow;

		[Params(StorageMode.Ram, StorageMode.Disk)]
		public StorageMode Storage { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_engine = BenchmarkLogs.Open(Records, this.Storage);

			// The middle of the log, as after scrolling.
			_firstRow = Records / 2;
		}

		[Benchmark]
		public int Screen()
		{
			RecordList records = _engine.Filter.Results;
			var length = 0;

			for (var row = _firstRow; row < _firstRow + RowsOnScreen; row++)
			{
				IRecord record = records[row];

				for (var read = 0; read < ReadsPerRow; read++)
				{
					length += record.Content.Length;
				}
			}

			return length;
		}
	}
}
