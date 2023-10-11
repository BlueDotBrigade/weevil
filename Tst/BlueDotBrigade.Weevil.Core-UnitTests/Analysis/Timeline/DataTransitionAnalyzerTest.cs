namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class DataTransitionAnalyzerTest
	{
		private IFilterAliasExpander _anyFilterAliasExpander;

		[TestInitialize]
		public void Setup()
		{
			var expander = Substitute.For<IFilterAliasExpander>();
			expander.Expand(Arg.Any<string>()).Returns((string s) => s);
			expander.Expand(Arg.Any<string>()).Returns((string[] s) => s);

			_anyFilterAliasExpander = expander;
		}
	}
}
