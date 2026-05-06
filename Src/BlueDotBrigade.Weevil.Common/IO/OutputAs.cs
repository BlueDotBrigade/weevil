namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;

	public static class OutputAs
	{
		public static IOutputFormatter ResolveFormatter(IReadOnlyList<string> arguments, IOutputFormatter fallbackFormatter)
		{
			if (arguments is null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			if (fallbackFormatter is null)
			{
				throw new ArgumentNullException(nameof(fallbackFormatter));
			}

			return TryGetValue(arguments, out var format) && TryCreateFormatter(format, out var formatter)
				? formatter
				: fallbackFormatter;
		}

		public static string[] RemoveFromArguments(IReadOnlyList<string> arguments)
		{
			if (arguments is null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			var sanitized = new List<string>(arguments.Count);

			for (var index = 0; index < arguments.Count; index++)
			{
				var argument = arguments[index];

				if (!TryGetOptionValue(argument, out _, out var hasInlineValue))
				{
					sanitized.Add(argument);
					continue;
				}

				if (!hasInlineValue
				    && index + 1 < arguments.Count
				    && !LooksLikeOption(arguments[index + 1]))
				{
					index++;
				}
			}

			return sanitized.ToArray();
		}

		private static bool TryGetValue(IReadOnlyList<string> arguments, out string value)
		{
			value = string.Empty;

			for (var index = 0; index < arguments.Count; index++)
			{
				var argument = arguments[index];

				if (!TryGetOptionValue(argument, out var inlineValue, out var hasInlineValue))
				{
					continue;
				}

				if (hasInlineValue)
				{
					if (IsValidValue(inlineValue))
					{
						value = inlineValue;
					}

					continue;
				}

				if (index + 1 >= arguments.Count || LooksLikeOption(arguments[index + 1]))
				{
					continue;
				}

				var nextArgument = arguments[++index];
				if (IsValidValue(nextArgument))
				{
					value = nextArgument;
				}
			}

			return value.Length > 0;
		}

		private static bool TryCreateFormatter(string value, out IOutputFormatter formatter)
		{
			var normalized = Normalize(value);

			switch (normalized)
			{
				case "markdown":
				case "md":
					formatter = new MarkdownFormatter();
					return true;
				case "plaintext":
				case "plain":
				case "text":
				case "txt":
					formatter = new PlainTextFormatter();
					return true;
				case "html":
					formatter = new HtmlFormatter();
					return true;
				case "json":
					formatter = new JsonFormatter();
					return true;
				case "xml":
					formatter = new XmlFormatter();
					return true;
				default:
					formatter = null!;
					return false;
			}
		}

		private static bool TryGetOptionValue(string argument, out string value, out bool hasInlineValue)
		{
			value = string.Empty;
			hasInlineValue = false;

			if (argument.StartsWith("--output-as=", StringComparison.OrdinalIgnoreCase)
			    || argument.StartsWith("--outputas=", StringComparison.OrdinalIgnoreCase))
			{
				hasInlineValue = true;
				value = argument.Substring(argument.IndexOf('=') + 1);
				return true;
			}

			if (argument.StartsWith("/output-as:", StringComparison.OrdinalIgnoreCase)
			    || argument.StartsWith("/outputas:", StringComparison.OrdinalIgnoreCase))
			{
				hasInlineValue = true;
				value = argument.Substring(argument.IndexOf(':') + 1);
				return true;
			}

			return string.Equals(argument, "--output-as", StringComparison.OrdinalIgnoreCase)
			       || string.Equals(argument, "--outputas", StringComparison.OrdinalIgnoreCase)
			       || string.Equals(argument, "/output-as", StringComparison.OrdinalIgnoreCase)
			       || string.Equals(argument, "/outputas", StringComparison.OrdinalIgnoreCase);
		}

		private static bool LooksLikeOption(string argument)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				return false;
			}

			return argument.StartsWith("-", StringComparison.Ordinal)
			       || argument.StartsWith("/", StringComparison.Ordinal);
		}

		private static bool IsValidValue(string value) => !string.IsNullOrWhiteSpace(value);

		private static string Normalize(string value)
		{
			return value
				.Replace("-", string.Empty, StringComparison.Ordinal)
				.Replace("_", string.Empty, StringComparison.Ordinal)
				.Replace(" ", string.Empty, StringComparison.Ordinal)
				.Trim()
				.ToLowerInvariant();
		}
	}
}
