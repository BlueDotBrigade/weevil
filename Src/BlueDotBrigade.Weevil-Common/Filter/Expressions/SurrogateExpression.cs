namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using Data;

	public sealed class SurrogateExpression : IExpression
	{
		private static readonly SurrogateExpression _instance = new SurrogateExpression();

		static SurrogateExpression()
		{
			// static constructor required
			// ... to tell C# compiler not to mark type as beforefieldinit
			// ... https://csharpindepth.com/articles/singleton
		}

		private SurrogateExpression()
		{
			// nothing to do
		}

		public static IExpression Instance => _instance;

		public static bool IsSurrogateOrNull(IExpression expression)
		{
			if (expression == null)
			{
				return true;
			}
			else
			{
				return _instance.Equals(expression);
			}
		}

		public static bool IsReal(IExpression expression)
		{
			return !IsSurrogateOrNull(expression);
		}

		public bool IsMatch(IRecord record)
		{
			return false;
		}
	}
}
