namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Globalization;
	using Diagnostics;

	internal class TsvRecordParser : IRecordParser
	{
		private const char FieldDelimiter = '\t';

		public bool TryParse(int line, string content, out IRecord record)
		{
			var isValidRecord = false;

			record = Record.Dummy;

			const int ExpectedFieldCount = 5;

			if (!string.IsNullOrWhiteSpace(content))
			{
				// The fields are located as ranges, so no string is allocated for each field (#967).
				// When there are more delimiters, the last range holds the rest of the line.
				ReadOnlySpan<char> text = content.AsSpan();
				Span<Range> fields = stackalloc Range[ExpectedFieldCount];

				isValidRecord = text.Split(fields, FieldDelimiter) >= ExpectedFieldCount;

				if (isValidRecord)
				{
					// TryParse methods are used so that malformed values, including numbers that are
					// too large, never throw an exception (#959).
					var isTimestampValid = DateTime.TryParseExact(
						text[fields[0]],
						"yyyy-MM-dd HH:mm:ss.FFFFF", CultureInfo.InvariantCulture, DateTimeStyles.None,
						out DateTime createdAt);

					var isProcessIdValid = int.TryParse(text[fields[1]],
						NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
						CultureInfo.InvariantCulture,
						out _);

					var isThreadIdValid = int.TryParse(text[fields[2]],
						NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
						CultureInfo.InvariantCulture,
						out var threadId);

					if (isTimestampValid && isProcessIdValid && isThreadIdValid)
					{
						SeverityTypeHelpers.TryParse(text[fields[3]], out SeverityType severityType);

						record = new Record(line, createdAt, severityType, content, isMultiLine: false, wasGeneratedByUi: threadId == 1);
					}
					else
					{
						Log.Default.Write(LogSeverityType.Warning,
							$"Unable to parse record. Line={line}, IsTimestampValid={isTimestampValid}, IsProcessIdValid={isProcessIdValid}, IsThreadIdValid={isThreadIdValid}");
					}
				}
			}

			return Record.IsGenuine(record);
		}
	}
}