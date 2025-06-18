namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using Data;

	internal class ElapsedTimeAnalyzer
	{
		private readonly ImmutableArray<IRecord> _records;

		public ElapsedTimeAnalyzer(ImmutableArray<IRecord> records)
		{
			_records = records;
		}

		public void Analyze()
		{
			IRecord previous = Record.Dummy;

			foreach (IRecord current in _records)
			{
				current.Metadata.ElapsedTime = Metadata.ElapsedTimeUnknown;

				if (Record.IsDummyOrNull(previous))
				{
					if (current.HasCreationTime)
					{
						previous = current;
					}
				}
				else
				{
					if (current.HasCreationTime)
					{
						current.Metadata.ElapsedTime = current.CreatedAt - previous.CreatedAt;
						previous = current;
					}
				}
			}
		}
	}
}
