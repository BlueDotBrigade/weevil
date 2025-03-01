namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using BlueDotBrigade.Weevil.Data;

	public class SimpleCallStackFormatter : IRecordFormatter
	{
		private static readonly Regex FilePathPattern;
		private static readonly Regex[] CallStackPatterns;

		static SimpleCallStackFormatter()
		{
			var options = RegexOptions.Compiled | RegexOptions.Multiline;

			FilePathPattern = new Regex(
				@"in [a-zA-Z]:[\\\/].*\.cs:line",
				options);

			// Note: The "end of line assertion" ($) in .NET does not consume the carriage return & line feed (\r\n).
			// https://stackoverflow.com/a/8618642/949681
			var patterns = new List<Regex>()
			{
				new Regex(@"^\s+at System\..*\r?\n?", options),
				new Regex(@"^\s+at Microsoft\..*\r?\n?", options),
				new Regex(@"^\s+at MS.Win32\..*\r?\n?", options),
				new Regex(@"^\s+at MS.Internal\..*\r?\n?", options),
				new Regex(@"^\s+at FluentAssertions\..*\r?\n?", options),
				new Regex(@"^\s+at TechTalk.SpecFlow\..*\r?\n?", options),
				new Regex(@"^\s+at Reqnroll\..*\r?\n?", options),
				new Regex(@"^\s+at Boa.Constrictor.Screenplay\..*\r?\n?", options),
				new Regex(@"^.*--- End of inner exception stack trace ---.*\r?\n?", options),
				new Regex(@"^.*--- End of stack trace from previous location where exception was thrown ---.*\r?\n?", options),
			};
			CallStackPatterns = patterns.ToArray();
		}

		public string Format(IRecord record)
		{
			var result = record.Content;

			if (record.Metadata.IsMultiLine)
			{
				foreach (var pattern in CallStackPatterns)
				{
					result = pattern.Replace(result, string.Empty);
				}

				var filePaths = FilePathPattern
					.Matches(result)
					.Cast<Match>()
					.Select(m => m.Value)
					.ToArray();

				foreach (var filePath in filePaths)
				{
					result = result.Replace(filePath, "on line");
				}
			}

			var wasSucessful = result.Length < record.Content.Length;

			return result;
		}
	}
}
