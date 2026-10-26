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
		private readonly IAnnotations _annotations;

		public UserCommentExpression(string serializedExpression, IAnnotations annotations)
		{
			_annotations = annotations;
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

		public bool IsMatch(IRecord record)
		{
			var isMatch = false;
			var comment = _annotations.GetComment(record.LineNumber);

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
