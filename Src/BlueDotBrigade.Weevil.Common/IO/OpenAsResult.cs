namespace BlueDotBrigade.Weevil.IO
{
	using System;

	/// <summary>
	/// What a plugin's own Open As window returned, before Weevil's Open As dialog replaced it (#1216).
	/// </summary>
	/// <remarks>
	/// Kept only so that a plugin that still has a <c>ShowOpenAs</c> method builds. Weevil no longer calls it: a plugin shows its own options
	/// in Weevil's Open As dialog through <c>BlueDotBrigade.Weevil.Windows.IO.IOpenAsOptionsPlugin</c>.
	/// </remarks>
	[Obsolete("Weevil no longer shows a plugin's own Open As window. A plugin supplies its options through IOpenAsOptionsPlugin, in BlueDotBrigade.Weevil.Windows (#1216).")]
	public class OpenAsResult
	{
		public OpenAsResult()
		{
			this.Context = new ContextDictionary();
			this.Range = Range.All;
		}

		public OpenAsResult(ContextDictionary context, Range range)
		{
			this.Context = context;
			this.Range = range;
		}

		/// <summary>
		/// Represents metadata that is specific to the file format of the open document (e.g. file format version).
		/// </summary>
		/// <remarks>
		/// The <see cref="ContextDictionary"/> may be empty because these parameters are
		/// specific to the <see cref="IPlugin"/> that is being used.
		/// </remarks>
		public ContextDictionary Context { get; }

		/// <summary>
		/// Represents the line numbers that will be loaded.
		/// </summary>
		public Range Range { get; }
	}
}
