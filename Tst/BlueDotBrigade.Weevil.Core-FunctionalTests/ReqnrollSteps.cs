namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	internal abstract class ReqnrollSteps
	{
		private readonly Token _token;

		public ReqnrollSteps(Token token)
		{
			_token = token;	
		}

		internal Token Token => _token;
	}
}