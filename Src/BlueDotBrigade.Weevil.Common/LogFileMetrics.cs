namespace BlueDotBrigade.Weevil
{
	using System;

	public class LogFileMetrics
	{
		public LogFileMetrics(long fileSize, int count, TimeSpan sourceFileLoadingPeriod, TimeSpan recordAndMetadataLoadTime)
		{
			this.FileSize = fileSize;
			this.RecordCount = count;

			this.SourceFileLoadingPeriod = sourceFileLoadingPeriod;
			this.RecordAndMetadataLoadDuration = recordAndMetadataLoadTime;
		}

		public long FileSize { get; }
		public int RecordCount { get; }
		public TimeSpan SourceFileLoadingPeriod { get; }
		public TimeSpan RecordAndMetadataLoadDuration { get; }
	}
}
