namespace BlueDotBrigade.Weevil.TestTools.Configuration.Reqnroll
{
	using System;
	
	public static class Constants
	{
		/// <summary>
		/// Ensures that the Reqnroll hook runs before all other hooks.
		/// </summary>
		/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html#hook-execution-order">Reqnroll: Execution Order</seealso>
		public const int AlwaysFirst = 0;

		/// <summary>
		/// Ensures that the Reqnroll hook runs after all other hooks.
		/// </summary>
		/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html#hook-execution-order">Reqnroll: Execution Order</seealso>
		public const int AlwaysLast = Int32.MaxValue;
	}
}
