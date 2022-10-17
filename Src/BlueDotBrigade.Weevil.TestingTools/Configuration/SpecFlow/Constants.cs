namespace BlueDotBrigade.Weevil.TestingTools.Configuration.SpecFlow
{
	using System;
	
	public static class Constants
	{
		/// <summary>
		/// Ensures that the SpecFlow hook runs before all other hooks.
		/// </summary>
		/// <seealso href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html#hook-execution-order">SpecFlow: Execution Order</seealso>
		public const int AlwaysFirst = 0;

		/// <summary>
		/// Ensures that the SpecFlow hook runs after all other hooks.
		/// </summary>
		/// <seealso href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html#hook-execution-order">SpecFlow: Execution Order</seealso>
		public const int AlwaysLast = Int32.MaxValue;
	}
}
