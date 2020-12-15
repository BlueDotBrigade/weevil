namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Analysis;
	using Data;

	public interface ISelect
	{
		IDictionary<int, IRecord> Selected { get; }
		bool IsTimePeriodSelected { get; }
		TimeSpan TimePeriodOfInterest { get; }
		ISelect Select(int lineNumber);
		ISelect Select(IRecord record);
		ISelect Select(IList<IRecord> records);
		ISelect Unselect(int lineNumber);
		ISelect Unselect(IRecord record);
		ISelect Unselect(IList<IRecord> records);
		ISelect SaveSelection(string destinationFolder, FileFormatType fileFormatType);
		ImmutableArray<IRecord> ClearAll();
		ImmutableArray<IRecord> GetSelected();
		void ToggleIsPinned();
	}
}