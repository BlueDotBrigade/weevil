namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;

	/// <summary>
	/// Represents statistical metrics for a graph series.
	/// </summary>
	public class SeriesMetrics
	{
		public SeriesMetrics(
			string seriesName,
			int count,
			double? min,
			double? max,
			double? mean,
			double? median,
			DateTime? rangeStart,
			DateTime? rangeEnd)
		{
			this.SeriesName = seriesName;
			this.Count = count;
			this.Min = min;
			this.Max = max;
			this.Mean = mean;
			this.Median = median;
			this.RangeStart = rangeStart;
			this.RangeEnd = rangeEnd;
		}

		public string SeriesName { get; }
		public int Count { get; }
		public double? Min { get; }
		public double? Max { get; }
		public double? Mean { get; }
		public double? Median { get; }
		public DateTime? RangeStart { get; }
		public DateTime? RangeEnd { get; }

		/// <summary>
		/// Returns a formatted string representation suitable for display.
		/// </summary>
		public string MinFormatted => Min.HasValue ? Min.Value.ToString("0.000") : "N/A";
		public string MaxFormatted => Max.HasValue ? Max.Value.ToString("0.000") : "N/A";
		public string MeanFormatted => Mean.HasValue ? Mean.Value.ToString("0.000") : "N/A";
		public string MedianFormatted => Median.HasValue ? Median.Value.ToString("0.000") : "N/A";
		public string RangeStartFormatted => RangeStart.HasValue ? RangeStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";
		public string RangeEndFormatted => RangeEnd.HasValue ? RangeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";
	}
}
