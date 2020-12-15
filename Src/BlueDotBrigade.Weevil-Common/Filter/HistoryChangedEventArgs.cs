namespace BlueDotBrigade.Weevil.Filter
{
	using System;

	public class HistoryChangedEventArgs : EventArgs
	{
		public HistoryChangedEventArgs(HistoryChangeType changeType, int index, string value)
		{
			this.ChangeType = changeType;
			this.Index = index;
			this.Value = value;
		}

		public HistoryChangeType ChangeType { get; }
		public int Index { get; }
		public string Value { get; }
	}
}
