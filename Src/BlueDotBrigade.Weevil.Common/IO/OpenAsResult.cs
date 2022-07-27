namespace BlueDotBrigade.Weevil.IO
{
	using System;

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
