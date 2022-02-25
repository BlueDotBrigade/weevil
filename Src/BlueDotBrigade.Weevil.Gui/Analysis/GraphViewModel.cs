namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using LiveChartsCore;
	using LiveChartsCore.Defaults;
	using LiveChartsCore.Kernel.Sketches;
	using LiveChartsCore.SkiaSharpView;

	public class GraphViewModel
	{
		private static readonly NumberStyles ValidNumberStyle =
			NumberStyles.AllowLeadingWhite |
			NumberStyles.AllowTrailingWhite |
			NumberStyles.AllowThousands |
			NumberStyles.Integer |
			NumberStyles.AllowExponent |
			NumberStyles.AllowDecimalPoint;

		private readonly ImmutableArray<IRecord> _records;

		public GraphViewModel(ImmutableArray<IRecord> records, string regExPattern)
		{
			_records = records;
			regExPattern = @"Threads.Count=(?<ThreadCount>\d+)";

			this.Series = GetSeries(records, new RegularExpression(regExPattern));
			this.XAxes = GetXAxes("Value Recorded At");
			this.YAxes = GetYAxes("Handle Count");
		}

		public IEnumerable<ISeries> Series { get; set; }
		public IEnumerable<ICartesianAxis> XAxes { get; set; }

		public IEnumerable<ICartesianAxis> YAxes { get; set; }

		private static IEnumerable<ICartesianAxis> GetXAxes(string name)
		{
			return new Axis[]
			{
				new Axis
				{
					Name = name,
					Labeler = point => new DateTime((long)point).ToString("hh:mm:ss"),
					LabelsRotation = 15,

					//// in this case we want our columns with a width of 1 day, we can get that number
					//// using the following syntax
					UnitWidth = TimeSpan.FromSeconds(1).Ticks, // mark

					//// The MinStep property forces the separator to be greater than 1 day.
					MinStep = TimeSpan.FromSeconds(1).Ticks // mark

					// if the difference between our points is in hours then we would:
					// UnitWidth = TimeSpan.FromHours(1).Ticks,

					// since all the months and years have a different number of days
					// we can use the average, it would not cause any visible error in the user interface
					// Months: TimeSpan.FromDays(30.4375).Ticks
					// Years: TimeSpan.FromDays(365.25).Ticks
				}
			};
		}

		private static IEnumerable<ICartesianAxis> GetYAxes(string name)
		{
			return new Axis[]
			{
				new Axis
				{
					Name = name
				}
			};
		}

		private static IEnumerable<ISeries> GetSeries(ImmutableArray<IRecord> records, RegularExpression expression)
		{
			var parsingError = false;
			var values = new ObservableCollection<DateTimePoint>();

			foreach (var record in records)
			{
				var matches = expression.GetKeyValuePairs(record);

				if (matches.Any())
				{
					if (float.TryParse(matches.First().Value, ValidNumberStyle, CultureInfo.InvariantCulture,
							out var value))
					{
						values.Add(new DateTimePoint(record.CreatedAt, value));
					}
					else
					{
						if (!parsingError)
						{
							Log.Default.Write(
								LogSeverityType.Warning,
								$"Unable to graph the datapoint because the matching value is not a float. {matches.First().Value}");
							parsingError = true;
						}
					}

				}
			}

			return new ISeries[]
			{
				new LineSeries<DateTimePoint>
				{
					Name = "Handle Count",
					Values = values,
					GeometrySize = 10,
				}
			};
		}
	}
}
