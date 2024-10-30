namespace BlueDotBrigade.Weevil.Gui
{
	using System.Collections.Generic;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using System.Windows;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using NSubstitute;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal abstract class ReqnrollSteps
	{
		private readonly Token _context;

		public ReqnrollSteps(Token context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		internal Token Context => _context;
	}
}
