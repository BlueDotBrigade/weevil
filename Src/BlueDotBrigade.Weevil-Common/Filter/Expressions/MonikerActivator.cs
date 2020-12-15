namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System;

	public class MonikerActivator
	{
		public Moniker Moniker { get; }
		public Func<string, IExpression> Create { get; }

		public MonikerActivator(Moniker moniker, Func<string, IExpression> create)
		{
			this.Moniker = moniker;
			this.Create = create;
		}
	}
}