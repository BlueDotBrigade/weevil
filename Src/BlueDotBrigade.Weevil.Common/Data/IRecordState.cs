namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// Gets and sets the state of one record: its comment, pin and flag, and its elapsed time (decision 7 of the #990 plan).
	/// </summary>
	/// <remarks>
	/// Records hold no state of their own. The caller does not need to know where each value is kept:
	/// comments and pins belong to the log and are shared by every view of it, while flags and elapsed times belong to one view.
	/// </remarks>
	public interface IRecordState
	{
		string GetComment(IRecord record);

		/// <summary>
		/// The record has a comment that is not only white space.
		/// </summary>
		bool HasComment(IRecord record);

		bool IsPinned(IRecord record);

		bool IsFlagged(IRecord record);

		/// <summary>
		/// The time since the previous record the view shows, or <see cref="ElapsedTimes.Unknown"/>.
		/// </summary>
		TimeSpan GetElapsedTime(IRecord record);

		/// <summary>
		/// Adding a comment pins the record, and removing it unpins the record.
		/// </summary>
		void SetComment(IRecord record, string comment);

		/// <summary>
		/// Adds <paramref name="comment"/> to the end of the record's comment, after a comma, or sets it when the record has none.
		/// </summary>
		void AppendComment(IRecord record, string comment);

		void SetPinned(IRecord record, bool isPinned);

		void SetFlagged(IRecord record, bool isFlagged);
	}
}
