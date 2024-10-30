namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

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
