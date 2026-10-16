namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Globalization;
	using Diagnostics;

	internal class TsvRecordParser : IRecordParser
	{
		private static readonly char[] FieldDelimiter = new[] { '\t' };

		public bool TryParse(int line, string content, out IRecord record)
		{
			var isValidRecord = false;

			record = Record.Dummy;

			const int ExpectedFieldCount = 5;

			if (!string.IsNullOrWhiteSpace(content))
			{
				var fields = content.Split(FieldDelimiter, ExpectedFieldCount);

				isValidRecord = fields.Length >= ExpectedFieldCount;

				if (isValidRecord)
				{
					// TryParse methods are used so that malformed values, including numbers that are
					// too large, never throw an exception (#959).
					var isTimestampValid = DateTime.TryParseExact(
						fields[0],
						"yyyy-MM-dd HH:mm:ss.FFFFF", CultureInfo.InvariantCulture, DateTimeStyles.None,
						out DateTime createdAt);

					var isProcessIdValid = int.TryParse(fields[1],
						NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
						CultureInfo.InvariantCulture,
						out _);

					var isThreadIdValid = int.TryParse(fields[2],
						NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
						CultureInfo.InvariantCulture,
						out var threadId);

					if (isTimestampValid && isProcessIdValid && isThreadIdValid)
					{
						var metadata = new Metadata();

						if (threadId == 1)
						{
							metadata.WasGeneratedByUi = true;
						}

						SeverityTypeHelpers.TryParse(fields[3], out SeverityType severityType);

						record = new Record(line, createdAt, severityType, content, metadata);
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