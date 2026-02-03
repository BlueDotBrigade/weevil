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
	using BlueDotBrigade.Weevil.IO;
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
		private static readonly int MaxSeriesCount = 4;
		private static readonly string DefaultSeries2Suffix = " 2";
		private static readonly int MetricsPrecision = 3;

		// Y-Axis position options for each series
		public static readonly string YAxisLeft = "Left";
		public static readonly string YAxisRight = "Right";

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
		private ObservableCollection<SeriesMetrics> _seriesMetrics;

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
		private string _series3Name;
		private string _series4Name;
		private string _series1Axis = YAxisLeft;
		private string _series2Axis = YAxisLeft;
		private string _series3Axis = YAxisLeft;
		private string _series4Axis = YAxisLeft;

		public GraphViewModel(ImmutableArray<IRecord> records, string regularExpression, string windowTitle, string sourceFilePath)
		{
			_records = records;

			this.WindowTitle = windowTitle ?? DefaultWindowTitle;
			this.SourceFilePath = sourceFilePath ?? string.Empty;
			this.TooltipWidth = 10;
			this.RegularExpression = regularExpression ?? string.Empty;
			this.SeriesMetrics = new ObservableCollection<SeriesMetrics>();

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

		public string Series3Name
		{
			get => _series3Name;
			set
			{
				if (_series3Name != value)
				{
					_series3Name = value;
					RaisePropertyChanged(nameof(this.Series3Name));
				}
			}
		}

		public string Series4Name
		{
			get => _series4Name;
			set
			{
				if (_series4Name != value)
				{
					_series4Name = value;
					RaisePropertyChanged(nameof(this.Series4Name));
				}
			}
		}

		public string Series1Axis
		{
			get => _series1Axis;
			set
			{
				if (_series1Axis != value)
				{
					_series1Axis = value;
					RaisePropertyChanged(nameof(this.Series1Axis));
					Update(false);
				}
			}
		}

		public string Series2Axis
		{
			get => _series2Axis;
			set
			{
				if (_series2Axis != value)
				{
					_series2Axis = value;
					RaisePropertyChanged(nameof(this.Series2Axis));
					Update(false);
				}
			}
		}

		public string Series3Axis
		{
			get => _series3Axis;
			set
			{
				if (_series3Axis != value)
				{
					_series3Axis = value;
					RaisePropertyChanged(nameof(this.Series3Axis));
					Update(false);
				}
			}
		}

		public string Series4Axis
		{
			get => _series4Axis;
			set
			{
				if (_series4Axis != value)
				{
					_series4Axis = value;
					RaisePropertyChanged(nameof(this.Series4Axis));
					Update(false);
				}
			}
		}

		public ObservableCollection<SeriesMetrics> SeriesMetrics
		{
			get => _seriesMetrics;
			set
			{
				_seriesMetrics = value;
				RaisePropertyChanged(nameof(this.SeriesMetrics));
			}
		}

		public ICommand UpdateCommand => new UiBoundCommand(() => Update(false));

		public ICommand CopyMetricsCommand => new UiBoundCommand(() => CopyMetricsToClipboard());

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
						if (seriesNames.Count > 2)
						{
							this.Series3Name = seriesNames[2];
						}
						if (seriesNames.Count > 3)
						{
							this.Series4Name = seriesNames[3];
						}
					}
				}

				this.Series = GetSeries(_records, this.RegularExpression, 
				new[] { this.Series1Name, this.Series2Name, this.Series3Name, this.Series4Name }, 
				new[] { this.Series1Axis, this.Series2Axis, this.Series3Axis, this.Series4Axis });

				this.XAxes = GetXAxes(this.XAxisLabel, TimeSpan.FromSeconds(this.TooltipWidth));
				
				var seriesList = this.Series.ToList();
				
				// Collect all series names and axes
				var allSeriesNames = new[] { this.Series1Name, this.Series2Name, this.Series3Name, this.Series4Name };
				var allSeriesAxes = new[] { this.Series1Axis, this.Series2Axis, this.Series3Axis, this.Series4Axis };
				
				// Determine if we need dual axes - when any series is on the right axis
				bool needsDualAxes = seriesList.Count > 1 && allSeriesAxes.Any(axis => axis == YAxisRight);
				
				if (needsDualAxes)
				{
					// Build axis names by combining series names that are on each axis
					var leftSeriesNames = new List<string>();
					var rightSeriesNames = new List<string>();
					
					for (int i = 0; i < MaxSeriesCount; i++)
					{
						if (i < seriesList.Count && !string.IsNullOrEmpty(allSeriesNames[i]))
						{
							if (allSeriesAxes[i] == YAxisLeft)
							{
								leftSeriesNames.Add(allSeriesNames[i]);
							}
							else
							{
								rightSeriesNames.Add(allSeriesNames[i]);
							}
						}
					}
					
					string leftAxisName = leftSeriesNames.Any() 
						? string.Join(" / ", leftSeriesNames) 
						: DefaultYAxisLabel;
					string rightAxisName = rightSeriesNames.Any() 
						? string.Join(" / ", rightSeriesNames) 
						: DefaultYAxisLabel;
					
					this.YAxes = GetYAxes(leftAxisName, rightAxisName);
				}
				else
				{
					// Single axis - use the combined name of all series or first available
					var allNames = new[] { this.Series1Name, this.Series2Name, this.Series3Name, this.Series4Name }
						.Where(name => !string.IsNullOrEmpty(name))
						.Take(seriesList.Count);
					string axisName = allNames.Any() ? string.Join(" / ", allNames) : DefaultYAxisLabel;
					this.YAxes = GetYAxes(axisName);
				}

				// Calculate and update metrics
				this.SeriesMetrics = CalculateSeriesMetrics(this.Series);
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

		private static bool TryGetMatchForRecord(ImmutableArray<RegularExpression> expressions, string inputString, out float[] values)
		{
			values = new float[MaxSeriesCount];
			for (int i = 0; i < MaxSeriesCount; i++)
			{
				values[i] = float.NaN;
			}
			
			var hasAnyValue = false;

			if (!expressions.IsDefaultOrEmpty &&
			    !string.IsNullOrEmpty(inputString))
			{
				if (expressions.Length == 1)
				{
					IDictionary<string, string> matches = expressions[0].GetKeyValuePairs(inputString);

					if (matches.Any())
					{
						var matchList = matches.Take(MaxSeriesCount).ToList();
						for (var index = 0; index < matchList.Count && index < MaxSeriesCount; index++)
						{
							if (float.TryParse(matchList[index].Value, NumberStyle, CultureInfo.InvariantCulture, out var value))
							{
								values[index] = value;
								hasAnyValue = true;
							}
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
							if (float.TryParse(matchValue, NumberStyle, CultureInfo.InvariantCulture, out var value))
							{
								values[index] = value;
								hasAnyValue = true;
							}
						}
					}
				}
			}

			return hasAnyValue;
		}

		private static List<string> GetSeriesNames(string inputString, ImmutableArray<RegularExpression> expressions)
		{
			if (expressions.IsDefaultOrEmpty)
			{
				return new List<string> { DefaultSeriesName };
			}

			// When we have a single expression, it might have multiple named groups
			// So we need to allocate space for up to MaxSeriesCount series
			var seriesCount = (expressions.Length == 1) ? MaxSeriesCount : Math.Min(expressions.Length, MaxSeriesCount);
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
			// NOTE: GetGroupNameOrDefault should only be called after all records are processed
			// in the calling method GetSeriesNames(ImmutableArray<IRecord>, string), not here.
			// Calling it here fills in default names prematurely, preventing actual group names
			// from being extracted from subsequent records.
			// GetGroupNameOrDefault(seriesNames);
			return seriesNames;
		}

		private static List<string> GetSeriesNames(ImmutableArray<IRecord> records, string regularExpression)
		{
			var expressions = ParseRegularExpressions(regularExpression);

			if (expressions.IsDefaultOrEmpty)
			{
				return new List<string> { DefaultSeriesName };
			}

			// When we have a single expression, it might have multiple named groups
			// So we need to allocate space for up to MaxSeriesCount series
			var seriesCount = (expressions.Length == 1) ? MaxSeriesCount : Math.Min(expressions.Length, MaxSeriesCount);
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

		private static IEnumerable<ISeries> GetSeries(ImmutableArray<IRecord> records, string regularExpression, string[] seriesNames, string[] seriesAxes)
		{
			var seriesValues = new ObservableCollection<DateTimePoint>[MaxSeriesCount];
			var hasSeriesData = new bool[MaxSeriesCount];
			
			for (int i = 0; i < MaxSeriesCount; i++)
			{
				seriesValues[i] = new ObservableCollection<DateTimePoint>();
				hasSeriesData[i] = false;
			}
			
			var expressions = ParseRegularExpressions(regularExpression);

			if (records.Length > 0)
			{
				foreach (IRecord record in records)
				{
					try
					{
						if (TryGetMatchForRecord(expressions, record.Content, out var values))
						{
							for (int i = 0; i < MaxSeriesCount; i++)
							{
								if (!float.IsNaN(values[i]))
								{
									seriesValues[i].Add(new DateTimePoint(record.CreatedAt, values[i]));
									hasSeriesData[i] = true;
								}
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

			var seriesList = new List<ISeries>();

			// Create series for each that has data
			for (int i = 0; i < MaxSeriesCount; i++)
			{
				if (hasSeriesData[i])
				{
					var finalSeriesName = !string.IsNullOrEmpty(seriesNames[i]) 
						? seriesNames[i] 
						: $"{DefaultSeriesName} {i + 1}";
					
					// Determine which axis this series should be on
					// Left = axis 0, Right = axis 1
					int axisIndex = seriesAxes[i] == YAxisRight ? 1 : 0;
					
					seriesList.Add(new LineSeries<DateTimePoint>
					{
						Name = finalSeriesName,
						Values = seriesValues[i],
						GeometrySize = 10,
						ScalesYAt = axisIndex,
						TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} at {chartPoint.Model.DateTime:hh:mm:ss} was {chartPoint.PrimaryValue.ToString(FloatFormat)}",
					});
				}
			}

			return seriesList;
		}

		/// <summary>
		/// Calculates statistical metrics for all series.
		/// </summary>
		private static ObservableCollection<SeriesMetrics> CalculateSeriesMetrics(
			IEnumerable<ISeries> series)
		{
			var metricsList = new ObservableCollection<SeriesMetrics>();

			foreach (var s in series)
			{
				if (s is LineSeries<DateTimePoint> lineSeries)
				{
					var points = lineSeries.Values.Cast<DateTimePoint>().ToList();
					
					if (points.Any())
					{
						// Filter out null values to avoid skewing statistics
						var values = points
							.Where(p => p.Value.HasValue)
							.Select(p => p.Value.Value)
							.ToList();
						var timestamps = points.Select(p => p.DateTime).ToList();
						
						if (values.Any())
						{
							var count = values.Count;
							var min = values.Min();
							var max = values.Max();
							var mean = values.Average();
							
							// Calculate median
							var sortedValues = values.OrderBy(v => v).ToList();
							var mid = sortedValues.Count / 2;
							var median = (sortedValues.Count % 2 == 0)
								? (sortedValues[mid - 1] + sortedValues[mid]) / 2.0
								: sortedValues[mid];
							
							var rangeStart = timestamps.Min();
							var rangeEnd = timestamps.Max();
							
							var metrics = new SeriesMetrics(
								lineSeries.Name ?? "Unknown",
								count,
								min,
								max,
								Math.Round(mean, MetricsPrecision),
								Math.Round(median, MetricsPrecision),
								rangeStart,
								rangeEnd);
							
							metricsList.Add(metrics);
						}
						else
						{
							// All values were null
							var metrics = new SeriesMetrics(
								lineSeries.Name ?? "Unknown",
								0,
								null,
								null,
								null,
								null,
								null,
								null);
							
							metricsList.Add(metrics);
						}
					}
					else
					{
						// Empty series
						var metrics = new SeriesMetrics(
							lineSeries.Name ?? "Unknown",
							0,
							null,
							null,
							null,
							null,
							null,
							null);
						
						metricsList.Add(metrics);
					}
				}
			}

			return metricsList;
		}

		/// <summary>
		/// Serializes the metrics data as tab-delimited text suitable for clipboard copying.
		/// </summary>
		public string SerializeMetrics()
		{
			return SerializeMetrics(new PlainTextFormatter());
		}

		/// <summary>
		/// Serializes the metrics data using the specified output formatter.
		/// </summary>
		/// <param name="formatter">The formatter to use for output formatting.</param>
		/// <returns>Formatted metrics data as a string.</returns>
		public string SerializeMetrics(IOutputFormatter formatter)
		{
			if (formatter == null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			if (this.SeriesMetrics == null || !this.SeriesMetrics.Any())
			{
				return string.Empty;
			}

			// Prepare headers
			var headers = new[]
			{
				"Series Name",
				"Count",
				"Min",
				"Max",
				"Mean",
				"Median",
				"Range Start",
				"Range End"
			};

			// Prepare data rows
			var rows = new string[this.SeriesMetrics.Count][];
			for (int i = 0; i < this.SeriesMetrics.Count; i++)
			{
				var metrics = this.SeriesMetrics[i];
				rows[i] = new[]
				{
					metrics.SeriesName,
					metrics.Count.ToString(),
					metrics.MinFormatted,
					metrics.MaxFormatted,
					metrics.MeanFormatted,
					metrics.MedianFormatted,
					metrics.RangeStartFormatted,
					metrics.RangeEndFormatted
				};
			}

			return formatter.AsTable(headers, rows);
		}

		/// <summary>
		/// Copies the metrics data to the clipboard.
		/// </summary>
		private void CopyMetricsToClipboard()
		{
			try
			{
				var serializedData = SerializeMetrics();
				if (!string.IsNullOrEmpty(serializedData))
				{
					Clipboard.SetData(DataFormats.UnicodeText, serializedData);
				}
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Failed to copy metrics to clipboard.");
				MessageBox.Show("Failed to copy metrics to clipboard.", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
