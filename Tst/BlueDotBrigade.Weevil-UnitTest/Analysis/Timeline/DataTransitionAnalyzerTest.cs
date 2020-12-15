namespace BlueDotBrigade.Weevil.Analysis
{
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class DataTransitionAnalyzerTest
	{
		private IStaticAliasExpander _anyStaticAliasExpander;

		[TestInitialize]
		public void Setup()
		{
			var expander = new Mock<IStaticAliasExpander>();
			expander.Setup(x => x.Expand(It.IsAny<string>())).Returns((string s) => s);
			expander.Setup(x => x.Expand(It.IsAny<string[]>())).Returns((string[] s) => s);

			_anyStaticAliasExpander = expander.Object;
		}
	}
}
