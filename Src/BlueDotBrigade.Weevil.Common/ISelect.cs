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
		/// The line a range of lines is selected from, for example by Shift+click, or -1 when there is none.
		/// </summary>
		/// <remarks>
		/// A filter that hides the anchor's record drops the anchor, as it drops the selected records it hides.
		/// </remarks>
		int AnchorLineNumber { get; }

		/// <summary>
		/// Replaces the selection with every record the filter shows from <paramref name="anchorLineNumber"/> to <paramref name="lineNumber"/>, in either order.
		/// </summary>
		/// <remarks>
		/// <paramref name="anchorLineNumber"/> becomes the <see cref="AnchorLineNumber"/>, and <paramref name="lineNumber"/> the active record.
		/// When <paramref name="anchorLineNumber"/> is -1 (no anchor), <paramref name="lineNumber"/> is used instead.
		/// The cost does not depend on the number of records.
		/// </remarks>
		ISelect SelectOnly(int anchorLineNumber, int lineNumber);

		/// <summary>
		/// Adds every record the filter shows from <paramref name="anchorLineNumber"/> to <paramref name="lineNumber"/>, in either order, to the selection.
		/// </summary>
		/// <remarks>
		/// <paramref name="anchorLineNumber"/> becomes the <see cref="AnchorLineNumber"/>, and <paramref name="lineNumber"/> the active record.
		/// When <paramref name="anchorLineNumber"/> is -1 (no anchor), <paramref name="lineNumber"/> is used instead.
		/// The cost does not depend on the number of records.
		/// </remarks>
		ISelect SelectRange(int anchorLineNumber, int lineNumber);

		/// <summary>
		/// Selects the record if it is not selected, and unselects it otherwise. The record becomes the anchor and the active record.
		/// </summary>
		/// <exception cref="RecordNotFoundException">The filter hides the record.</exception>
		ISelect Toggle(int lineNumber);

		/// <summary>
		/// Selects every record the filter shows. The anchor and the active record do not change.
		/// </summary>
		ISelect SelectAll();

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
		ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType);
		ImmutableArray<IRecord> ClearAll();
		ImmutableArray<IRecord> GetSelected();
		void ToggleIsPinned();

		/// <summary>
		/// Puts <paramref name="comment"/> on the selected records that have none, or on every selected record when <paramref name="allowOverwrite"/> is <see langword="true"/>.
		/// </summary>
		/// <remarks>
		/// A record whose comment is only white space has none. Adding a comment pins the record.
		/// </remarks>
		void SetComment(string comment, bool allowOverwrite);
	}
}