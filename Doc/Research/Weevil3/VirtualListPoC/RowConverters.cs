// The converters of Weevil's row template, from Src/BlueDotBrigade.Weevil.Gui/Converters on main at 1b128ae,
// with one change each: they take a RowViewModel instead of an IRecord, as #990 step 5 (#1015) must do.
// The two formatters come from Src/BlueDotBrigade.Weevil.Common, changed the same way.

#nullable enable

namespace Weevil.VirtualListPoC;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BlueDotBrigade.Weevil;

[ValueConversion(typeof(SeverityType), typeof(SolidColorBrush))]
public sealed class SeverityToBackgroundConverter : IValueConverter
{
	public SolidColorBrush? WarningBrush { get; set; }

	public SolidColorBrush? ErrorBrush { get; set; }

	public SolidColorBrush? CriticalBrush { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return SeverityBrushes.Select(value, this.WarningBrush, this.ErrorBrush, this.CriticalBrush);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

[ValueConversion(typeof(SeverityType), typeof(SolidColorBrush))]
public sealed class SeverityToForegroundConverter : IValueConverter
{
	public SolidColorBrush? WarningBrush { get; set; }

	public SolidColorBrush? ErrorBrush { get; set; }

	public SolidColorBrush? CriticalBrush { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return SeverityBrushes.Select(value, this.WarningBrush, this.ErrorBrush, this.CriticalBrush);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

[ValueConversion(typeof(TimeSpan), typeof(string))]
public sealed class TimeSpanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		TimeSpan timeSpanValue = value is TimeSpan t ? t : TimeSpan.MinValue;
		return timeSpanValue.ToHumanReadable();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Ensures that the record content is not exceptionally long, and simplifies call stacks.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class ContentConverter : IValueConverter
{
	private const int MaximumLength = 8 * 1024;
	private const int TruncatedLength = 1 * 256;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var result = DependencyProperty.UnsetValue;

		if (value is RowViewModel row)
		{
			var content = RowFormatters.SimplifyCallStack(row.Content, row.IsMultiLine);
			result = RowFormatters.Shorten(content, MaximumLength, TruncatedLength);
		}

		return result;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

[ValueConversion(typeof(string), typeof(string))]
public sealed class ContentToTruncationIconConverter : IValueConverter
{
	private static readonly ContentConverter ContentConverter = new ContentConverter();

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var result = DependencyProperty.UnsetValue;

		if (value is RowViewModel row)
		{
			var convertedText = ContentConverter.Convert(value, targetType, parameter, culture);
			result = row.Content == (convertedText as string)
				? string.Empty
				: " ... ";
		}

		return result;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// A quick hack for an unexpected validation error: https://github.com/BlueDotBrigade/weevil/issues/58
/// </summary>
public sealed class CheckBoxValidationErrorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is bool booleanValue && booleanValue;
	}
}

/// <summary>
/// Expects the row, the window's view-model, and optionally <c>True</c> for a tool tip.
/// </summary>
public sealed class RegionStringConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is null || values.Length < 2 || values[0] is not RowViewModel row || values[1] is not MainViewModel viewModel)
		{
			return string.Empty;
		}

		var isToolTip = values.Length == 3 && bool.TryParse(values[2]?.ToString(), out var flag) && flag;

		if (viewModel.RegionStartsWith(row, out var startName))
		{
			return isToolTip ? $"🡇 Start of region: {startName}" : $"🡇 {startName} ";
		}

		if (viewModel.RegionEndsWith(row, out var endName))
		{
			return isToolTip ? $"🡅 End of region: {endName}" : $"🡅 {endName} ";
		}

		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Expects the row, the window's view-model, and optionally <c>True</c> for a tool tip.
/// </summary>
public sealed class BookmarkStringConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is null || values.Length < 2 || values[0] is not RowViewModel row || values[1] is not MainViewModel viewModel)
		{
			return string.Empty;
		}

		var isToolTip = values.Length >= 3 && bool.TryParse(values[2]?.ToString(), out var flag) && flag;

		if (!viewModel.TryGetBookmark(row, out var id, out var name))
		{
			return string.Empty;
		}

		// Weevil's BookmarkSymbol picks a symbol from the name; one symbol is enough here.
		const string symbol = "🔖";
		var bookmarkText = id > 0 ? $"{id} : {name} " : $"{name} ";

		if (isToolTip)
		{
			return id > 0 ? $"Bookmark: {name} (Ctrl+{id})" : $"Bookmark: {name}";
		}

		return (parameter as string)?.ToLowerInvariant() switch
		{
			"symbol" => symbol,
			"details" => bookmarkText,
			_ => $"{symbol} {bookmarkText}",
		};
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

internal static class SeverityBrushes
{
	public static object Select(object value, SolidColorBrush? warning, SolidColorBrush? error, SolidColorBrush? critical)
	{
		return value is SeverityType severity
			? severity switch
			{
				SeverityType.Warning => warning ?? Brushes.Transparent,
				SeverityType.Error => error ?? Brushes.Transparent,
				SeverityType.Critical => critical ?? Brushes.Transparent,
				_ => DependencyProperty.UnsetValue,
			}
			: DependencyProperty.UnsetValue;
	}
}

/// <summary>
/// Weevil's <c>SimpleCallStackFormatter</c> and <c>ShortenedRecordFormatter</c>, taking the content instead of a record.
/// </summary>
internal static class RowFormatters
{
	private const string EndOfLine = "⋯";

	private static readonly Regex FilePathPattern;
	private static readonly Regex[] CallStackPatterns;

	static RowFormatters()
	{
		RegexOptions options = RegexOptions.Compiled | RegexOptions.Multiline;
		FilePathPattern = new Regex(@"in [a-zA-Z]:[\\\/].*\.cs:line", options);
		CallStackPatterns = new[]
		{
			new Regex(@"^\s+at System\..*\r?\n?", options),
			new Regex(@"^\s+at Microsoft\..*\r?\n?", options),
			new Regex(@"^\s+at MS.Win32\..*\r?\n?", options),
			new Regex(@"^\s+at MS.Internal\..*\r?\n?", options),
			new Regex(@"^\s+at FluentAssertions\..*\r?\n?", options),
			new Regex(@"^\s+at TechTalk.SpecFlow\..*\r?\n?", options),
			new Regex(@"^\s+at Reqnroll\..*\r?\n?", options),
			new Regex(@"^\s+at Boa.Constrictor.Screenplay\..*\r?\n?", options),
			new Regex(@".*--- End of inner exception stack trace", options),
			new Regex(@".*--- End of stack trace from previous location\r?\n?", options),
		};
	}

	public static string SimplifyCallStack(string content, bool isMultiLine)
	{
		var result = content;
		if (isMultiLine)
		{
			foreach (Regex pattern in CallStackPatterns)
			{
				result = pattern.Replace(result, string.Empty);
			}

			foreach (var filePath in FilePathPattern.Matches(result).Select(m => m.Value).ToArray())
			{
				result = result.Replace(filePath, "on line");
			}
		}

		return result;
	}

	public static string Shorten(string content, int maximumLength, int truncatedLength)
	{
		return content.Length >= maximumLength
			? content.Substring(0, truncatedLength) + EndOfLine
			: content;
	}
}
