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
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using BlueDotBrigade.Weevil.Gui.Input;
	using LiveChartsCore;
	using LiveChartsCore.Defaults;
	using LiveChartsCore.Kernel.Sketches;
	using LiveChartsCore.SkiaSharpView;

	public class GraphViewModel : INotifyPropertyChanged
	{
		private static readonly string DefaultSeriesName = "Series";
		private static readonly string DefaultXAxisLabel = "Time";
		private static readonly string DefaultYAxisLabel = "Y-Axis";
		private static readonly string FloatFormat = "0.000";

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

		private string _xAxisLabel;
		private string _yAxisLabel;

		private string _regularExpression;
		private string _dataDetected;
		private string _sampleData;
		private int _tooltipWidth;

		public GraphViewModel(ImmutableArray<IRecord> records, string regularExpression)
		{
			_records = records;

			this.TooltipWidth = 10;
			this.RegularExpression = regularExpression ?? string.Empty;

			this.SampleData = records.Any()
				? _records[0].Content
				: string.Empty;

			this.DataDetected = GetDetectedData(this.RegularExpression, this.SampleData);

			Update(true);

			this.XAxisLabel = this.XAxes.First().Name;
			this.YAxisLabel = this.YAxes.First().Name;
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
				return _xAxisLabel;
			}
			set
			{
				if (_xAxisLabel != value)
				{
					_xAxisLabel = value;
					RaisePropertyChanged(nameof(this.XAxisLabel));
				}
			}
		}

		public string YAxisLabel
		{
			get
			{
				return _yAxisLabel;
			}
			set
			{
				if (_yAxisLabel != value)
				{
					_yAxisLabel = value;
					RaisePropertyChanged(nameof(this.YAxisLabel));
				}
			}
		}

		public string DataDetected
		{
			get => _dataDetected;
			set
			{
				if (_dataDetected != value)
				{
					_dataDetected = value;
					RaisePropertyChanged(nameof(this.DataDetected));
				}
			}
		}

		public string SampleData
		{
			get => _sampleData;
			set
			{
				if (_sampleData != value)
				{
					_sampleData = value;
					RaisePropertyChanged(nameof(this.SampleData));
				}
			}
		}

		public int TooltipWidth
		{
			get => _tooltipWidth;
			set
			{
				if (_tooltipWidth != value)
				{
					if (value > 0)
					{
						_tooltipWidth = value;
						RaisePropertyChanged(nameof(this.TooltipWidth));
					}
				}
			}
		}

		public string RegularExpression
		{
			get => _regularExpression;
			set
			{
				if (_regularExpression != value)
				{
					_regularExpression = value;
					RaisePropertyChanged(nameof(this.RegularExpression));

					this.DataDetected = GetDetectedData(this.RegularExpression, this.SampleData);
				}
			}
		}

		public ICommand UpdateCommand => new UiBoundCommand(() => Update(false));

		private string GetDetectedData(string regularExpression, string inputString)
		{
			var result = string.Empty;

			try
			{
				if (TryGetMatch(
					    regularExpression,
					    inputString,
					    out var detectedValue))
				{
					return detectedValue.ToString(FloatFormat);
				}
				else
				{
					return string.Empty;
				}
			}
			catch (InvalidExpressionException e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Unexpected error occurred while trying to parse sample data for graph.");
				result = "(invalid expression)";
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Unexpected error occurred while trying to parse sample data for graph.");
				result = "(unknown)";
			}

			return result;
		}

		private void Update(bool isInitializing)
		{
			try
			{
				this.Series = GetSeries(_records, this.RegularExpression);

				this.XAxes = GetXAxes(this.XAxisLabel, TimeSpan.FromSeconds(this.TooltipWidth));
				this.YAxes = GetYAxes(isInitializing ? this.Series.First().Name : this.YAxisLabel);
			}
			catch (MatchCountException e)
			{
				var message =
					$"{e.Message}\r\n\r\n" +
					$"Expression: {e.Expression}";
				Log.Default.Write(LogSeverityType.Error, message);
				MessageBox.Show(message, "Regular Expression Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			catch (InvalidExpressionException e)
			{
				var reason = e.InnerException != null
					? e.InnerException.Message
					: "Unknown";

				var message =
					$"{e.Message}\r\n\r\n" +
					$"Reason: {reason}";
				Log.Default.Write(LogSeverityType.Error, e, "Unexpected error occurred while trying to parse series.");
				MessageBox.Show(message, "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private static IEnumerable<ICartesianAxis> GetXAxes(string name, TimeSpan unitWidth)
		{
			return new Axis[]
			{
				new Axis
				{
					Name = name ?? DefaultXAxisLabel,
					Labeler = point => new DateTime((long)point).ToString("hh:mm:ss"),
					LabelsRotation = 15,

					//// in this case we want our columns with a width of 1 day, we can get that number
					//// using the following syntax
					UnitWidth = unitWidth.Ticks, // mark

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
					Name = name ?? DefaultYAxisLabel,
				}
			};
		}

		private static bool TryGetMatch(string regularExpression, string inputString, out float value)
		{
			var wasSuccessful = false;
			value = float.NaN;

			if (!string.IsNullOrEmpty(regularExpression) &&
			    !string.IsNullOrEmpty(inputString))
			{
				var expression = new RegularExpression(regularExpression);
				IDictionary<string, string> matches = expression.GetKeyValuePairs(inputString);

				if (matches.Any())
				{
					if (float.TryParse(matches.First().Value, NumberStyle, CultureInfo.InvariantCulture, out value))
					{
						wasSuccessful = true;
					}
				}
			}

			return wasSuccessful;
		}

		private static IEnumerable<ISeries> GetSeries(ImmutableArray<IRecord> records, string regularExpression)
		{
			var seriesName = DefaultSeriesName;
			var values = new ObservableCollection<DateTimePoint>();

			if (records.Length > 0)
			{
				foreach (IRecord record in records)
				{
					try
					{
						if (TryGetMatch(regularExpression, record.Content, out var value))
						{
							values.Add(new DateTimePoint(record.CreatedAt, value));
						}
					}
					catch (InvalidExpressionException e)
					{
						throw new InvalidExpressionException(
							e.Expression,
							$"Cannot parse the record on line {record.LineNumber}.", 
							e);
					}
				}

				seriesName = GetSeriesName(records.First().Content, new RegularExpression(regularExpression));
			}

			return new ISeries[]
			{
					new LineSeries<DateTimePoint>
					{
						Name = seriesName,
						Values = values,
						GeometrySize = 10,
						TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} at {chartPoint.Model.DateTime:hh:mm:ss} was {chartPoint.PrimaryValue.ToString(FloatFormat)}",
					}
			};
		}

		private static string GetSeriesName(string inputString, RegularExpression expression)
		{
			var seriesName = DefaultSeriesName;

			var matches = expression.GetKeyValuePairs(inputString);
			if (matches.Any())
			{
				seriesName = matches.First().Key;
			}

			return seriesName;
		}
	}
}
