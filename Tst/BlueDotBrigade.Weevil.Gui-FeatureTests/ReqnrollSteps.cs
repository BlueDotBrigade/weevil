namespace BlueDotBrigade.Weevil.Gui
{
	using System.Collections.Generic;
	using System.Windows;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal abstract class ReqnrollSteps
	{
		private readonly Token _context;

		private static readonly Logger _logWriter = NLog.LogManager.GetCurrentClassLogger();

		public ReqnrollSteps(Token context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		internal Token Context => _context;

		internal Logger LogWriter => _logWriter;
	}
}
