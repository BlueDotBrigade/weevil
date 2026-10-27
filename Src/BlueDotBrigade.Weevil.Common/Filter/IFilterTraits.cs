namespace BlueDotBrigade.Weevil.Filter
{
	using System.Collections.Generic;

	public interface IFilterTraits
	{
		/// <summary>
		/// The include filters used most recently, newest first. Only the filter adds to it (#1109).
		/// </summary>
		IReadOnlyList<string> IncludeHistory { get; }

		/// <summary>
		/// The exclude filters used most recently, newest first. Only the filter adds to it (#1109).
		/// </summary>
		IReadOnlyList<string> ExcludeHistory { get; }
		FilterType FilterType { get; }
		IFilterCriteria Criteria { get; }
	}
}
