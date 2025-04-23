namespace BlueDotBrigade.Weevil.Data
{
	using System;

	internal class DefaultRecordParser : IRecordParser
	{
		private readonly MetadataManager _metadataManager;

		public DefaultRecordParser(MetadataManager metadataManager)
		{
			_metadataManager = metadataManager;
		}

		public bool TryParse(int line, string content, out IRecord record)
		{
			record = new Record(line, DateTime.MaxValue, SeverityType.Information, content, _metadataManager);

			return Record.IsGenuine(record);
		}
	}
}