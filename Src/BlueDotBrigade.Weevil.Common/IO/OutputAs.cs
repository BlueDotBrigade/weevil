namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;

	public static class OutputAs
	{
		/// <summary>
		/// The values that <c>--output-as</c> accepts, as an error message names them.
		/// </summary>
		private const string SupportedValues = "markdown, plaintext or json";

		/// <summary>
		/// The format that <c>--output-as</c> names, or the fallback when the option is missing.
		/// </summary>
		/// <exception cref="ArgumentException">The option names a format that Weevil does not write, such as <c>xml</c> (#837).</exception>
		public static OutputFormat ResolveFormat(IReadOnlyList<string> arguments, OutputFormat fallbackFormat)
		{
			if (arguments is null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			if (!TryGetValue(arguments, out var value))
			{
				return fallbackFormat;
			}

			return TryParseFormat(value, out var format)
				? format
				: throw new ArgumentException($"--output-as {value} is not supported. Use {SupportedValues}.");
		}

		/// <summary>
		/// The formatter for the text a command writes in the format.
		/// </summary>
		/// <remarks>
		/// A command that supports JSON renders it from its model, and writes it as a document that no formatter wraps; any other text is plain.
		/// </remarks>
		public static IOutputFormatter CreateFormatter(OutputFormat format)
		{
			return format switch
			{
				OutputFormat.Markdown => new MarkdownFormatter(),
				OutputFormat.PlainText => new PlainTextFormatter(),
				OutputFormat.Json => new PlainTextFormatter(),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, "The output format is not supported."),
			};
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

				index++;
				var nextArgument = arguments[index];
				if (IsValidValue(nextArgument))
				{
					value = nextArgument;
				}
			}

			return value.Length > 0;
		}

		private static bool TryParseFormat(string value, out OutputFormat format)
		{
			switch (Normalize(value))
			{
				case "markdown":
				case "md":
					format = OutputFormat.Markdown;
					return true;
				case "plaintext":
				case "plain":
				case "text":
				case "txt":
					format = OutputFormat.PlainText;
					return true;
				case "json":
					format = OutputFormat.Json;
					return true;
				default:
					format = default;
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
