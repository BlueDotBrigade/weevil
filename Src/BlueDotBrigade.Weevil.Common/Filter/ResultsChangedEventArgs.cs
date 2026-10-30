namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using Data;

	public class ResultsChangedEventArgs : EventArgs
	{
		public ResultsChangedEventArgs(RecordList records)
		{
			this.Records = records;
		}

		public RecordList Records { get; }
	}
}