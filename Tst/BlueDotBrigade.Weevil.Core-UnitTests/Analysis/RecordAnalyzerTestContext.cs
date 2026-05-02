namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Concurrent;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using NSubstitute;

	internal static class RecordAnalyzerTestContext
	{
		public static FilterStrategy CreateFilterStrategy()
		{
			var coreExtension = Substitute.For<ICoreExtension>();
			var context = new ContextDictionary();
			var filterAliasExpander = Substitute.For<IFilterAliasExpander>();
			filterAliasExpander.Expand(Arg.Any<string>()).Returns(x => x.Arg<string>());

			var filterCriteria = new FilterCriteria(string.Empty, string.Empty, new ConcurrentDictionary<string, object>());
			var regionManager = Substitute.For<IRegionManager>();
			var bookmarkManager = Substitute.For<IBookmarkManager>();

			return new FilterStrategy(
				coreExtension,
				context,
				filterAliasExpander,
				FilterType.RegularExpression,
				filterCriteria,
				regionManager,
				bookmarkManager);
		}

		public static IUserDialog CreateDialog(string regex, string analysisOrder = "Ascending")
		{
			var userDialog = Substitute.For<IUserDialog>();

			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(analysisOrder);

			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(callInfo =>
				{
					callInfo[2] = regex;
					return true;
				});

			return userDialog;
		}
	}
}
