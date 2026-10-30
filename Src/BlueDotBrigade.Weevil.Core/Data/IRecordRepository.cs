namespace BlueDotBrigade.Weevil.Data
{
	using System;

	public interface IRecordRepository
	{
		IRecord GetNext();

		RecordList GetAll();

		RecordList Get(int maximumCount);

		/// <summary>
		/// Returns all of the records that meet the specified criteria.
		/// </summary>
		/// <param name="range">Represents the line numbers that will be loaded.</param>
		/// <param name="maximumCount">Represents the maximum number of records that can be returned.</param>
		RecordList Get(Range range, int maximumCount);
	}
}