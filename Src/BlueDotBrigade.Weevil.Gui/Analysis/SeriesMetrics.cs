namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Windows.Media;

	/// <summary>
	/// Represents statistical metrics for a graph series.
	/// </summary>
	public class SeriesMetrics
	{
		private const string NumericFormat = "0.000";
		private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

		public SeriesMetrics(
			string seriesName,
			Color seriesColor,
			int count,
			double? min,
			double? max,
			double? mean,
			double? median,
			double? standardDeviation,
			DateTime? rangeStart,
			DateTime? rangeEnd)
		{
			this.SeriesName = seriesName;
			this.SeriesColor = seriesColor;
			this.Count = count;
			this.Min = min;
			this.Max = max;
			this.Mean = mean;
			this.Median = median;
			this.StandardDeviation = standardDeviation;
			this.RangeStart = rangeStart;
			this.RangeEnd = rangeEnd;
		}

		public string SeriesName { get; }
		public Color SeriesColor { get; }
		public int Count { get; }
		public double? Min { get; }
		public double? Max { get; }
		public double? Mean { get; }
		public double? Median { get; }
		public double? StandardDeviation { get; }
		public DateTime? RangeStart { get; }
		public DateTime? RangeEnd { get; }

		/// <summary>
		/// Returns a formatted string representation suitable for display.
		/// </summary>
		public string MinFormatted => Min.HasValue ? Min.Value.ToString(NumericFormat) : "N/A";
		public string MaxFormatted => Max.HasValue ? Max.Value.ToString(NumericFormat) : "N/A";
		public string MeanFormatted => Mean.HasValue ? Mean.Value.ToString(NumericFormat) : "N/A";
		public string MedianFormatted => Median.HasValue ? Median.Value.ToString(NumericFormat) : "N/A";
		public string StandardDeviationFormatted => StandardDeviation.HasValue ? StandardDeviation.Value.ToString(NumericFormat) : "N/A";
		public string RangeStartFormatted => RangeStart.HasValue ? RangeStart.Value.ToString(DateTimeFormat) : "N/A";
		public string RangeEndFormatted => RangeEnd.HasValue ? RangeEnd.Value.ToString(DateTimeFormat) : "N/A";
	}
}
