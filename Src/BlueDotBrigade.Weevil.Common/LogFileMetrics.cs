﻿namespace BlueDotBrigade.Weevil
{
	using System;

	public class LogFileMetrics
	{
		public LogFileMetrics(long fileSize, int count, TimeSpan recordLoadTime, TimeSpan recordAndMetadataLoadTime)
		{
			this.FileSize = fileSize;
			this.RecordCount = count;

			this.RecordLoadDuration = recordLoadTime;
			this.RecordAndMetadataLoadDuration = recordAndMetadataLoadTime;
		}

		public long FileSize { get; }
		public int RecordCount { get; }
		public TimeSpan RecordLoadDuration { get; }
		public TimeSpan RecordAndMetadataLoadDuration { get; }
	}
}
