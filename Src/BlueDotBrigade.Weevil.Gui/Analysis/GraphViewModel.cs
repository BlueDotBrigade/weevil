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
	using BlueDotBrigade.Weevil.Filter;
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
		private static readonly string DefaultWindowTitle = "Graph";
		private static readonly string FloatFormat = "0.000";
		private static readonly int MaxSeriesCount = 2;
		private static readonly string DefaultSeries2Suffix = " 2";

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
		private string _windowTitle;
		private string _sourceFilePath;

		private string _regularExpression;
		private string _dataDetected;
		private string _sampleData;
		private int _tooltipWidth;

		private string _series1Name;
		private string _series2Name;

		public GraphViewModel(ImmutableArray<IRecord> records, string regularExpression, string windowTitle, string sourceFilePath)
		{
			_records = records;

			this.WindowTitle = windowTitle ?? DefaultWindowTitle;
			this.SourceFilePath = sourceFilePath ?? string.Empty;
			this.TooltipWidth = 10;
			this.RegularExpression = regularExpression ?? string.Empty;

			this.SampleData = records.Any()
				? _records[0].Content
				: string.Empty;

			this.DataDetected = GetDetectedData(this.RegularExpression, this.SampleData);

			Update(true);

			this.XAxisLabel = this.XAxes.First().Name;
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

		public string WindowTitle
		{
			get
			{
				return _windowTitle;
			}
			set
			{
				if (_windowTitle != value)
				{
					_windowTitle = value;
					RaisePropertyChanged(nameof(this.WindowTitle));
				}
			}
		}

		public string SourceFilePath
		{
			get
			{
				return _sourceFilePath;
			}
			set
			{
				if (_sourceFilePath != value)
				{
					_sourceFilePath = value;
					RaisePropertyChanged(nameof(this.SourceFilePath));
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

		public string Series1Name
		{
			get => _series1Name;
			set
			{
				if (_series1Name != value)
				{
					_series1Name = value;
					RaisePropertyChanged(nameof(this.Series1Name));
				}
			}
		}

		public string Series2Name
		{
			get => _series2Name;
			set
			{
				if (_series2Name != value)
				{
					_series2Name = value;
					RaisePropertyChanged(nameof(this.Series2Name));
				}
			}
		}

		public ICommand UpdateCommand => new UiBoundCommand(() => Update(false));

		private string GetDetectedData(string regularExpression, string inputString)
		{
			var result = string.Empty;
			var expressions = ParseRegularExpressions(regularExpression);

			try
			{
				if (TryGetMatch(
					    expressions,
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
				// Initialize series names on first load from RegEx named groups
				// After initialization, series names are preserved even if RegEx changes,
				// allowing users to customize series names without them being overwritten
				if (isInitializing && _records.Length > 0)
				{
					var seriesNames = GetSeriesNames(_records, this.RegularExpression);
					if (seriesNames.Count > 0)
					{
						this.Series1Name = seriesNames[0];
						if (seriesNames.Count > 1)
						{
							this.Series2Name = seriesNames[1];
						}
					}
				}

				this.Series = GetSeries(_records, this.RegularExpression, this.Series1Name, this.Series2Name);

				this.XAxes = GetXAxes(this.XAxisLabel, TimeSpan.FromSeconds(this.TooltipWidth));
				
				var seriesList = this.Series.ToList();
				if (seriesList.Count > 1)
				{
					this.YAxes = GetYAxes(this.Series1Name, this.Series2Name);
				}
				else
				{
					this.YAxes = GetYAxes(this.Series1Name);
				}
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

		private static IEnumerable<ICartesianAxis> GetYAxes(string series1Name, string series2Name)
		{
			return new Axis[]
			{
				new Axis
				{
					Name = series1Name ?? DefaultYAxisLabel,
					Position = LiveChartsCore.Measure.AxisPosition.Start,
				},
				new Axis
				{
					Name = series2Name ?? DefaultYAxisLabel,
					Position = LiveChartsCore.Measure.AxisPosition.End,
				}
			};
		}

		private static ImmutableArray<RegularExpression> ParseRegularExpressions(string regularExpression)
		{
			if (string.IsNullOrWhiteSpace(regularExpression))
			{
				return ImmutableArray<RegularExpression>.Empty;
			}

			var expressions = new List<RegularExpression>();

			var segments = regularExpression.Split(
				new[] { Constants.FilterOrOperator },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var segment in segments)
			{
				var trimmedSegment = segment.Trim();
				if (!string.IsNullOrWhiteSpace(trimmedSegment))
				{
					expressions.Add(new RegularExpression(trimmedSegment));

					if (expressions.Count >= MaxSeriesCount)
					{
						break;
					}
				}
			}

			return expressions.ToImmutableArray();
		}

		private static void GetGroupNameOrDefault(List<string> seriesNames)
		{
			if (seriesNames.Count == 0)
			{
				seriesNames.Add($"{DefaultSeriesName} 1");
				return;
			}

			for (var index = 0; index < seriesNames.Count; index++)
			{
				if (string.IsNullOrEmpty(seriesNames[index]))
				{
					seriesNames[index] = $"{DefaultSeriesName} {index + 1}";
				}
			}
		}

		private static bool TryGetMatch(ImmutableArray<RegularExpression> expressions, string inputString, out float value)
		{
			var wasSuccessful = false;
			value = float.NaN;

			if (!expressions.IsDefaultOrEmpty &&
			    !string.IsNullOrEmpty(inputString))
			{
				foreach (var expression in expressions)
				{
					IDictionary<string, string> matches = expression.GetKeyValuePairs(inputString);

					if (matches.Any())
					{
						if (float.TryParse(matches.First().Value, NumberStyle, CultureInfo.InvariantCulture, out value))
						{
							wasSuccessful = true;
							break;
						}
					}
				}
			}

			return wasSuccessful;
		}

		private static bool TryGetMatchForRecord(ImmutableArray<RegularExpression> expressions, string inputString, out float value1, out float value2)
		{
			value1 = float.NaN;
			value2 = float.NaN;
			var hasFirstValue = false;
			var hasSecondValue = false;

			if (!expressions.IsDefaultOrEmpty &&
			    !string.IsNullOrEmpty(inputString))
			{
				if (expressions.Length == 1)
				{
					IDictionary<string, string> matches = expressions[0].GetKeyValuePairs(inputString);

					if (matches.Any())
					{
						var values = matches.Take(MaxSeriesCount).ToList();
						if (values.Count > 0)
						{
							hasFirstValue = float.TryParse(values[0].Value, NumberStyle, CultureInfo.InvariantCulture, out value1);
						}
						if (values.Count > 1)
						{
							hasSecondValue = float.TryParse(values[1].Value, NumberStyle, CultureInfo.InvariantCulture, out value2);
						}
					}
				}
				else
				{
					for (var index = 0; index < expressions.Length && index < MaxSeriesCount; index++)
					{
						IDictionary<string, string> matches = expressions[index].GetKeyValuePairs(inputString);

						if (matches.Any())
						{
							var matchValue = matches.First().Value;
							if (index == 0)
							{
								hasFirstValue = float.TryParse(matchValue, NumberStyle, CultureInfo.InvariantCulture, out value1);
							}
							else
							{
								hasSecondValue = float.TryParse(matchValue, NumberStyle, CultureInfo.InvariantCulture, out value2);
							}
						}
					}
				}
			}

			return hasFirstValue || hasSecondValue;
		}

		private static List<string> GetSeriesNames(string inputString, ImmutableArray<RegularExpression> expressions)
		{
			if (expressions.IsDefaultOrEmpty)
			{
				return new List<string> { DefaultSeriesName };
			}

			var seriesCount = Math.Min(expressions.Length, MaxSeriesCount);
			var seriesNames = Enumerable.Repeat(string.Empty, seriesCount).ToList();

			if (!string.IsNullOrEmpty(inputString))
			{
				if (expressions.Length == 1)
				{
					try
					{
						IDictionary<string, string> matches = expressions[0].GetKeyValuePairs(inputString);

						if (matches.Any())
						{
							var names = matches.Take(MaxSeriesCount).Select(m => m.Key).ToList();
							for (var index = 0; index < names.Count && index < seriesNames.Count; index++)
							{
								seriesNames[index] = names[index];
							}
						}
					}
					catch (Exception e)
					{
						Log.Default.Write(LogSeverityType.Warning, e, "Could not extract series names from regular expression.");
					}
				}
				else
				{
					for (var index = 0; index < expressions.Length && index < MaxSeriesCount; index++)
					{
						try
						{
							IDictionary<string, string> matches = expressions[index].GetKeyValuePairs(inputString);

							if (matches.Any())
							{
								seriesNames[index] = matches.First().Key;
							}
						}
						catch (Exception e)
						{
							Log.Default.Write(LogSeverityType.Warning, e, "Could not extract series names from regular expression.");
						}
					}
				}
			}
			GetGroupNameOrDefault(seriesNames);
			return seriesNames;
		}

		private static List<string> GetSeriesNames(ImmutableArray<IRecord> records, string regularExpression)
		{
			var expressions = ParseRegularExpressions(regularExpression);

			if (expressions.IsDefaultOrEmpty)
			{
				return new List<string> { DefaultSeriesName };
			}

			var seriesCount = Math.Min(expressions.Length, MaxSeriesCount);
			var seriesNames = Enumerable.Repeat(string.Empty, seriesCount).ToList();

			// Find records that match each expression to extract series names
			foreach (var record in records)
			{
				var recordSeriesNames = GetSeriesNames(record.Content, expressions);

				for (var index = 0; index < seriesNames.Count && index < recordSeriesNames.Count; index++)
				{
					if (string.IsNullOrEmpty(seriesNames[index]) && !string.IsNullOrEmpty(recordSeriesNames[index]))
					{
						seriesNames[index] = recordSeriesNames[index];
					}
				}

				var hasAllNames = true;
				for (var index = 0; index < seriesNames.Count; index++)
				{
					if (string.IsNullOrEmpty(seriesNames[index]))
					{
						hasAllNames = false;
						break;
					}
				}

				if (hasAllNames)
				{
					break;
				}
			}
			GetGroupNameOrDefault(seriesNames);
			return seriesNames;
		}

		private static IEnumerable<ISeries> GetSeries(ImmutableArray<IRecord> records, string regularExpression, string series1Name, string series2Name)
		{
			var values1 = new ObservableCollection<DateTimePoint>();
			var values2 = new ObservableCollection<DateTimePoint>();
			var hasSecondSeries = false;
			var expressions = ParseRegularExpressions(regularExpression);

			if (records.Length > 0)
			{
				foreach (IRecord record in records)
				{
					try
					{
						if (TryGetMatchForRecord(expressions, record.Content, out var value1, out var value2))
						{
							if (!float.IsNaN(value1))
							{
								values1.Add(new DateTimePoint(record.CreatedAt, value1));
							}
							if (!float.IsNaN(value2))
							{
								values2.Add(new DateTimePoint(record.CreatedAt, value2));
								hasSecondSeries = true;
							}
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
			}

			// Use provided names or defaults (Series N convention)
			var finalSeries1Name = !string.IsNullOrEmpty(series1Name) ? series1Name : $"{DefaultSeriesName} 1";
			var finalSeries2Name = !string.IsNullOrEmpty(series2Name) ? series2Name : $"{DefaultSeriesName} 2";

			var seriesList = new List<ISeries>
			{
				new LineSeries<DateTimePoint>
				{
					Name = finalSeries1Name,
					Values = values1,
					GeometrySize = 10,
					ScalesYAt = 0,
					TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} at {chartPoint.Model.DateTime:hh:mm:ss} was {chartPoint.PrimaryValue.ToString(FloatFormat)}",
				}
			};

			// Add second series if it has data
			if (hasSecondSeries)
			{
				seriesList.Add(new LineSeries<DateTimePoint>
				{
					Name = finalSeries2Name,
					Values = values2,
					GeometrySize = 10,
					ScalesYAt = 1,
					TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} at {chartPoint.Model.DateTime:hh:mm:ss} was {chartPoint.PrimaryValue.ToString(FloatFormat)}",
				});
			}

			return seriesList;
		}
	}
}
