namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using Data;

	public interface IRecordCounter
	{
		void Count(IRecord record);

		void Reset();

		IDictionary<string, object> GetResults();
	}
}
