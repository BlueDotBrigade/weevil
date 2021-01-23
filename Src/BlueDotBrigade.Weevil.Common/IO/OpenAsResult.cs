namespace BlueDotBrigade.Weevil.IO
{
	public class OpenAsResult
	{
		public OpenAsResult()
		{
			this.Context = new ContextDictionary();
			this.Range = Range.Complete;
		}

		public OpenAsResult(ContextDictionary context, Range range)
		{
			this.Context = context;
			this.Range = range;
		}

		public ContextDictionary Context { get; }
		public Range Range { get; }
	}
}
