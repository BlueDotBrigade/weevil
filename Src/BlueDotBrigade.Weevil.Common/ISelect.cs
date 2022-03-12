namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.Data;

	public interface ISelect
	{
		IDictionary<int, IRecord> Selected { get; }
		bool HasSelectionPeriod { get; }

		/// <summary>
		/// Represents the amount of time between the first and last selected records.
		/// </summary>
		TimeSpan SelectionPeriod { get; }
		/// <summary>
		/// The given <paramref name="lineNumber"/> is added to the "items of interest" list.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		ISelect Select(int lineNumber);
		/// <summary>
		/// The given <paramref name="record"/> is added to the "items of interest" list.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		ISelect Select(IRecord record);
		/// <summary>
		/// The given <paramref name="records"/> are added to the "items of interest" list.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
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