namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Immutable;
	using Data;

	public class ResultsChangedEventArgs : EventArgs
	{
		public ResultsChangedEventArgs(ImmutableArray<IRecord> records)
		{
			this.Records = records;
		}

		public ImmutableArray<IRecord> Records { get; }
	}
}