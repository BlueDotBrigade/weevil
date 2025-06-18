namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;

	public class Results
	{
		public static readonly Results None = new Results(0, NoData);

		private static readonly IDictionary<string, object> NoData = new Dictionary<string, object>();

		public int FlaggedRecords { get; init; }

		public IDictionary<string, object> Data { get; init; }

		public Results() : this(0, NoData)
		{
			// nothing to do
		}

		public Results(int flaggedRecords) : this(flaggedRecords, NoData)
		{
			// nothing to do
		}

		public Results(int flaggedRecords, IDictionary<string, object> data)
		{
			this.FlaggedRecords = 0;
			this.Data = data;
		}
	}
}
