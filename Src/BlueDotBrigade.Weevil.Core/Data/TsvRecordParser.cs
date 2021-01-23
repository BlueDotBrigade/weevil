namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Globalization;
	using Diagnostics;

	internal class TsvRecordParser : IRecordParser
	{
		private static readonly char[] FieldDelimiter = new[] { '\t' };

		public bool TryParse(int line, string content, out Record record)
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
					DateTime createdAt = DateTime.MaxValue;
					var metadata = new Metadata();

					try
					{
						createdAt = DateTime.ParseExact(
							fields[0],
							"yyyy-MM-dd HH:mm:ss.FFFFF", CultureInfo.InvariantCulture);

						var processId = int.Parse(fields[1],
							NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
							CultureInfo.InvariantCulture);

						var threadId = int.Parse(fields[2],
							NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
							CultureInfo.InvariantCulture);
						if (threadId == 1)
						{
							metadata.WasGeneratedByUi = true;
						}

						SeverityTypeHelpers.TryParse(fields[3], out SeverityType severityType);

						var context = fields[4];

						record = new Record(line, createdAt, severityType, content, metadata);
					}
					catch (FormatException e)
					{
						Log.Default.Write(LogSeverityType.Warning,
							$"Unable to parse record. Line={line}, Reason=`{e.Message}`");
					}
				}
			}

			return Record.IsGenuine(record);
		}
	}
}