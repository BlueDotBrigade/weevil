namespace BlueDotBrigade.Weevil.Data
{
	using System;

	internal class DefaultRecordParser : IRecordParser
	{
		public bool TryParse(int line, string content, out IRecord record)
		{
			record = new Record(line, DateTime.MaxValue, SeverityType.Information, content);

			return Record.IsGenuine(record);
		}
	}
}