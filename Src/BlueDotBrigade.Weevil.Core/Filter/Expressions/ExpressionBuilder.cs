namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System.Collections.Generic;
	using System.Collections.Immutable;

	internal class ExpressionBuilder
	{
		private readonly ImmutableArray<IExpressionFactory> _expressionFactories;

		private ExpressionBuilder(ImmutableArray<IExpressionFactory> expressionFactories)
		{
			_expressionFactories = expressionFactories;
		}

		public bool TryGetExpression(string serializedExpression, out IExpression result)
		{
			result = SurrogateExpression.Instance;

			foreach (IExpressionFactory factory in _expressionFactories)
			{
				if (factory.TryGetExpression(serializedExpression, out IExpression expression))
				{
					result = expression;
					break;
				}
			}

			return SurrogateExpression.IsReal(result);
		}

		public static ExpressionBuilder Create(ICoreExtension coreExtension, ContextDictionary context, FilterType filterType, IFilterCriteria criteria, IRegionManager regionManager)
		{
			var factories = new List<IExpressionFactory>();

			var builtInMonikers = new Monikers.ExpressionFactory(regionManager);

			factories.Add(builtInMonikers);

			IList<MonikerActivator> activators = coreExtension.GetMonikerActivators(context);
			factories.Add(new Monikers.ExpressionFactory(activators));

			switch (filterType)
			{
				case FilterType.PlainText:
					factories.Add(new PlainText.ExpressionFactory(criteria));
					break;

				case FilterType.RegularExpression:
					factories.Add(new Regular.ExpressionFactory(criteria));
					break;
			}


			return new ExpressionBuilder(ImmutableArray.Create(factories.ToArray()));
		}
	}
}
