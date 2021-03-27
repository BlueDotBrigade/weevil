namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil.Data;

	[ValueConversion(typeof(string), typeof(string))]
	public class ContentConverter : IValueConverter
	{
		private const int MaximumLength = 8 * 1024;
		private const int TrimmedLength = 1 * 256;

		internal static readonly string EndOfLine = "\u22EF";

		private static readonly Regex[] CallStackPatterns;

		static ContentConverter()
		{
			Debug.Assert(MaximumLength > TrimmedLength);

			var options = RegexOptions.Compiled | RegexOptions.Multiline;

			// Note: The "end of line assertion" ($) in .NET does not consume the carriage return & line feed (\r\n).
			// https://stackoverflow.com/a/8618642/949681
			var patterns = new List<Regex>()
			{
				new Regex(@"^\s+at System\..*\r?\n?", options),
				new Regex(@"^\s+at Microsoft\..*\r?\n?", options),
				new Regex(@"^\s+at MS.Win32\..*\r?\n?", options),
				new Regex(@"^\s+at MS.Internal\..*\r?\n?", options),
				new Regex(@"^.*--- End of inner exception stack trace ---.*\r?\n?", options),
				new Regex(@"^.*--- End of stack trace from previous location where exception was thrown ---.*\r?\n?", options),
			};
			CallStackPatterns = patterns.ToArray();
		}

		/// <summary>
		/// Ensures that the record content is not exceptionally long.
		/// </summary>
		/// <remarks>
		/// Converts data flowing from the source (e.g. DataContext/ViewModel) to the target (i.e. the user control).
		/// </remarks>
		public object Convert(object record, Type targetType, object isMultiLine, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (record != null)
			{
				if (record is IRecord concreteRecord)
				{
					if (TrySimplifyCallstack(concreteRecord.Content, concreteRecord.Metadata.IsMultiLine, out var smallerCallStack))
					{
						// further processing is not required
						result = smallerCallStack;
					}
					else
					{
						result = TruncateIf(concreteRecord.Content, !concreteRecord.Metadata.IsMultiLine, MaximumLength, TrimmedLength);
					}
				}
			}

			return result;
		}

		internal static bool TrySimplifyCallstack(string content, bool isMultiLine, out string newContent)
		{
			newContent = content;

			if (isMultiLine)
			{
				foreach (var pattern in CallStackPatterns)
				{
					newContent = pattern.Replace(newContent, string.Empty);
				}
			}

			return newContent.Length < content.Length;
		}

		internal static object TruncateIf(string content, bool isSingleLine, int maximumLength, int truncatedLength)
		{
			var result = content;

			if (isSingleLine)
			{
				if (content.Length >= maximumLength)
				{
					var maxLength = content.Length >= maximumLength
						? truncatedLength
						: content.Length;
					result = content.Substring(0, maxLength) + EndOfLine;
				}
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
