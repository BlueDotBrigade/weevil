namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class UserCommentExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Comment");

		private readonly bool _searchForSpecificText;
		private readonly string _searchValue;
		private readonly IRecordState _state;

		public UserCommentExpression(string serializedExpression, IRecordState state)
		{
			_state = state;
			_searchForSpecificText = true;
			_searchValue = string.Empty;

			if (Moniker.IsReferencedBy(serializedExpression))
			{
				if (Moniker.HasParameter(serializedExpression))
				{
					_searchForSpecificText = true;
					_searchValue = Moniker.GetParameter(serializedExpression);
				}
			}
		}

		public bool IsMatch(IRecord record, ContentBuffer content)
		{
			var isMatch = false;
			var comment = _state.GetComment(record);

			if (comment.Length > 0)
			{
				if (_searchForSpecificText)
				{
					var index = comment.IndexOf(_searchValue, StringComparison.InvariantCultureIgnoreCase);
					isMatch = index >= 0;
				}
				else
				{
					isMatch = true;
				}
			}

			return isMatch;
		}
	}
}
