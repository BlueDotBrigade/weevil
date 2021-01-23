namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System;

	public class Moniker
	{
		private readonly string _keyword;
		private readonly string _aliasWithParameter;

		/// <summary>
		/// A <see cref="BlueDotBrigade.Weevil.Filter.Filter"/> keyword that can be used to reference a built-in <see cref="IExpression"/>.
		/// </summary>
		/// <param name="keyword">A unique value that should only be assigned to one <see cref="IExpression"/> type.</param>
		public Moniker(string keyword)
		{
			_keyword = keyword;
			_aliasWithParameter = $"{keyword}=";
		}

		public bool IsReferencedBy(string serializedExpression)
		{
			return serializedExpression.StartsWith(_keyword, StringComparison.OrdinalIgnoreCase);
		}

		public bool HasParameter(string serializedExpression)
		{
			return serializedExpression.Length > _aliasWithParameter.Length;
		}

		public string GetParameter(string serializedExpression)
		{
			return serializedExpression.Substring(_aliasWithParameter.Length);
		}
	}
}
