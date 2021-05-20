namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	public class RawRecordFormatter : IRecordFormatter
	{
		public string Format(IRecord record)
		{
			if (record == null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			return record.Content;
		}
	}
}
