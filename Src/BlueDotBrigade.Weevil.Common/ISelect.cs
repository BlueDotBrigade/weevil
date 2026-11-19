namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;

	/// <summary>
	/// The selected records of the filter results, chosen by line number.
	/// </summary>
	/// <remarks>
	/// A line the filter hides is never selected: <see cref="Select"/>, <see cref="Unselect"/> and <see cref="Toggle"/> throw
	/// <see cref="RecordNotFoundException"/> and change nothing, and <see cref="SelectOnly"/> and <see cref="SelectRange"/> select
	/// only the records the filter shows in their range (#1249).
	/// </remarks>
	public interface ISelect
	{
		/// <summary>
		/// The number of selected records.
		/// </summary>
		int SelectedCount { get; }

		/// <summary>
		/// Returns <see langword="true"/> when the record on <paramref name="lineNumber"/> is selected.
		/// </summary>
		/// <remarks>
		/// It creates no record, so a grid can ask for each row it draws (#1137).
		/// </remarks>
		bool IsSelected(int lineNumber);

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
		/// Adds the shown line to the selection, and makes it the active record.
		/// </summary>
		/// <exception cref="RecordNotFoundException">The filter hides the line, or the log has no such line. The selection does not change.</exception>
		ISelect Select(int lineNumber);

		/// <summary>
		/// Removes the shown line from the selection.
		/// </summary>
		/// <exception cref="RecordNotFoundException">The filter hides the line, or the log has no such line. The selection does not change.</exception>
		ISelect Unselect(int lineNumber);

		ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType);

		/// <summary>
		/// As <see cref="SaveSelection(string, FileFormatType)"/>, and stops when <paramref name="cancellationToken"/> is cancelled, as Abort does (#1027).
		/// </summary>
		/// <remarks>
		/// A save that is cancelled, or fails, leaves an earlier file at <paramref name="destinationFilePath"/> as it was, and no partial file.
		/// </remarks>
		/// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType, System.Threading.CancellationToken cancellationToken);

		/// <summary>
		/// Returns the selected records, in the order of the filter results.
		/// </summary>
		/// <remarks>
		/// The list copies no records (#1140): one moment of the selection, which later changes leave as it is. <see cref="RecordList.GetLineNumber"/> reads a line number without creating a record.
		/// </remarks>
		RecordList GetSelected();
		void ToggleIsPinned();

		/// <summary>
		/// Puts <paramref name="comment"/> on the selected records that have none, or on every selected record when <paramref name="allowOverwrite"/> is <see langword="true"/>.
		/// </summary>
		/// <remarks>
		/// A record whose comment is only white space has none. Adding a comment pins the record.
		/// A <paramref name="comment"/> of only white space is not stored: with <paramref name="allowOverwrite"/>, it removes the selected records' comments.
		/// </remarks>
		void SetComment(string comment, bool allowOverwrite);
	}
}