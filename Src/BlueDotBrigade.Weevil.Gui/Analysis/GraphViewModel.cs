namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using LiveChartsCore;
	using LiveChartsCore.Defaults;
	using LiveChartsCore.Kernel.Sketches;
	using LiveChartsCore.SkiaSharpView;

	public class GraphViewModel : INotifyPropertyChanged
	{
		private static readonly string DefaultSeriesName = "Series";
		private static readonly string DefaultXAxisLabel = "Value Recorded At";
		private static readonly string DefaultYAxisLabel = "Y-Axis";

		private static readonly NumberStyles NumberStyle =
			NumberStyles.AllowLeadingWhite |
			NumberStyles.AllowTrailingWhite |
			NumberStyles.AllowThousands |
			NumberStyles.Integer |
			NumberStyles.AllowExponent |
			NumberStyles.AllowDecimalPoint;

		private readonly ImmutableArray<IRecord> _records;

		private IEnumerable<ISeries> _series;
		private IEnumerable<ICartesianAxis> _xAxes;
		private IEnumerable<ICartesianAxis> _yAxes;

		private string _regularExpression;

		private string _dataDetected;
		private string _sampleData;

		public GraphViewModel(ImmutableArray<IRecord> records, string regularExpression)
		{
			_records = records;

			this.RegularExpression = regularExpression ?? string.Empty;

			_sampleData = records.Any()
				? _records[0].Content
				: string.Empty;

			this.DataDetected = string.Empty;
			if (records.Any())
			{
				IDictionary<string, string> matches = new RegularExpression(this.RegularExpression)
					.GetKeyValuePairs(records.First());

				if (matches.Any())
				{
					this.DataDetected = matches.First().Value;
				}
			}

			this.Series = GetSeries(records, new RegularExpression(this.RegularExpression));
			this.XAxes = GetXAxes(DefaultXAxisLabel);
			this.YAxes = GetYAxes(DefaultYAxisLabel);
		}

		public IEnumerable<ISeries> Series
		{
			get => _series;
			set
			{
				_series = value;
				RaisePropertyChanged(nameof(this.Series));
			}
		}

		public IEnumerable<ICartesianAxis> XAxes
		{
			get => _xAxes;
			set
			{
				_xAxes = value;
				RaisePropertyChanged(nameof(this.XAxes));
			}
		}

		public IEnumerable<ICartesianAxis> YAxes
		{
			get => _yAxes;
			set
			{
				_yAxes = value;
				RaisePropertyChanged(nameof(this.YAxes));
			}
		}

		public string XAxisLabel
		{
			get
			{
				return this.XAxes.First().Name;
			}
			set
			{
				this.XAxes = GetXAxes(value);
			}
		}

		public string YAxisLabel
		{
			get
			{
				return this.YAxes.First().Name;
			}
			set
			{
				this.YAxes = GetYAxes(value);
			}
		}

		public string DataDetected
		{
			get => _dataDetected;
			set
			{
				_dataDetected = value;
				RaisePropertyChanged(nameof(this.DataDetected));
			}
		}

		public string SampleData
		{
			get => _sampleData;
			set
			{
				_sampleData = value;
				RaisePropertyChanged(nameof(this.SampleData));
			}
		}

		public string RegularExpression
		{
			get => _regularExpression;
			set
			{
				_regularExpression = value;
				RaisePropertyChanged(nameof(this.RegularExpression));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

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
					UnitWidth = TimeSpan.FromSeconds(10).Ticks, // mark

					//// The MinStep property forces the separator to be greater than 1 day.
					//MinStep = TimeSpan.FromSeconds(60).Ticks // mark
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
		private static bool TryGetMatch(RegularExpression expression, IRecord record, out float value)
		{
			var wasSuccessful = false;
			value = float.NaN;

			var matches = expression.GetKeyValuePairs(record);

			if (matches.Any())
			{
				if (float.TryParse(matches.First().Value, NumberStyle, CultureInfo.InvariantCulture, out value))
				{
					wasSuccessful = true;
				}
			}

			return wasSuccessful;
		}

		private static IEnumerable<ISeries> GetSeries(ImmutableArray<IRecord> records, RegularExpression expression)
		{
			var seriesName = DefaultSeriesName;
			var values = new ObservableCollection<DateTimePoint>();

			if (records.Length > 0)
			{
				foreach (IRecord record in records)
				{
					if (TryGetMatch(expression, record, out var value))
					{
						values.Add(new DateTimePoint(record.CreatedAt, value));
					}
				}

				var matches = expression.GetKeyValuePairs(records[0]);
				if (matches.Any())
				{
					seriesName = matches.First().Key;
				}
			}

			return new ISeries[]
			{
					new LineSeries<DateTimePoint>
					{
						Name = seriesName,
						Values = values,
						GeometrySize = 10,
						TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} at {chartPoint.Model.DateTime:hh:mm:ss} was {chartPoint.PrimaryValue:0.000}",
					}
			};
		}
	}
}
