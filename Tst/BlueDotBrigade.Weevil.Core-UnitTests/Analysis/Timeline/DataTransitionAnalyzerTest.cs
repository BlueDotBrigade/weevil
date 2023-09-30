namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class DataTransitionAnalyzerTest
	{
		private IFilterAliasExpander _anyFilterAliasExpander;

		//[TestInitialize]
		//public void Setup()
		//{
		//	var expander = Substitute.For<IFilterAliasExpander>();
		//	expander.Setup(x => x.Expand(It.IsAny<string>())).Returns((string s) => s);
		//	expander.Setup(x => x.Expand(It.IsAny<string[]>())).Returns((string[] s) => s);

		//	_anyFilterAliasExpander = expander.Object;
		//}
	}
}
