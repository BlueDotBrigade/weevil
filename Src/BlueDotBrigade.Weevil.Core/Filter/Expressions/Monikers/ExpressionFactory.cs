namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Filter.Expressions;

	internal class ExpressionFactory : IExpressionFactory
	{
		private readonly IList<MonikerActivator> _creators;

		public ExpressionFactory(IList<MonikerActivator> monikerActivators)
		{
			_creators = monikerActivators;
		}

		public ExpressionFactory(IRegionManager regionManager, IBookmarkManager bookmarkManager)
		{
			_creators = new List<MonikerActivator>
			{
				new MonikerActivator(FlagExpression.Moniker, (e) => new FlagExpression(e)),
				new MonikerActivator(PinnedExpression.Moniker, (e) => new PinnedExpression(e)),
				new MonikerActivator(IsMultiLineExpression.Moniker, (e) => new IsMultiLineExpression(e)),
				new MonikerActivator(SeverityTypeExpression.Moniker, (e) => new SeverityTypeExpression(e)),
				new MonikerActivator(LineNumberExpression.Moniker, (e) => new LineNumberExpression(e)),
				new MonikerActivator(UiThreadExpression.Moniker, (e) => new UiThreadExpression(e)),
				new MonikerActivator(UserCommentExpression.Moniker, (e) => new UserCommentExpression(e)),
				new MonikerActivator(ContentLengthExpression.Moniker, (e) => new ContentLengthExpression(e)),
				new MonikerActivator(ElapsedGreaterThanExpression.Moniker, (e) => new ElapsedGreaterThanExpression(e)),
				new MonikerActivator(RegionExpression.Moniker, (e) => new RegionExpression(e, regionManager)),
				new MonikerActivator(BookmarkExpression.Moniker, (e) => new BookmarkExpression(e, bookmarkManager)),
			};
		}

		public bool TryGetExpression(string serializedExpression, out IExpression result)
		{
			result = SurrogateExpression.Instance;

			if (!string.IsNullOrEmpty(serializedExpression))
			{
				foreach (MonikerActivator creator in _creators)
				{
					if (creator.Moniker.IsReferencedBy(serializedExpression))
					{
						result = creator.Create(serializedExpression);
						break;
					}
				}
			}

			return SurrogateExpression.IsReal(result);
		}
	}
}
