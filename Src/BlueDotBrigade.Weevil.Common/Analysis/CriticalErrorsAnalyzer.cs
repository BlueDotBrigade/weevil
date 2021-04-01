namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	public class CriticalErrorsAnalyzer : IRecordAnalyzer
	{
		public string Key { get; }
		public string DisplayName { get; }

		public int Count { get; private set; }

		public IRecord FirstOccurrence { get; private set; }

		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog)
		{
			foreach (var record in records)
			{
				if (record.Severity == SeverityType.Critical)
				{
					this.Count++;

					if (this.Count == 1)
					{
						this.FirstOccurrence = record;
					}
				}
			}

			return this.Count;
		}
	}
}
