namespace BlueDotBrigade.Weevil
{
	using System;

	public class LogFileMetrics
	{
		public LogFileMetrics(long fileSize, int count, TimeSpan recordLoadingPeriod, TimeSpan recordAndMetadataLoadTime)
		{
			this.FileSize = fileSize;
			this.RecordCount = count;

			this.RecordLoadingPeriod = recordLoadingPeriod;
			this.RecordAndMetadataLoadDuration = recordAndMetadataLoadTime;
		}

		public long FileSize { get; }
		public int RecordCount { get; }
		public TimeSpan RecordLoadingPeriod { get; }
		public TimeSpan RecordAndMetadataLoadDuration { get; }
	}
}
