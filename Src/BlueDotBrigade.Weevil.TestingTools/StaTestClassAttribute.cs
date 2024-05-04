namespace BlueDotBrigade.Weevil.TestingTools
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <credit authors="Gérald Barré (meziantou);" href="https://www.meziantou.net/mstest-v2-customize-test-execution.htm">
	///		<license type="None" href=""/>
	///		<comments>
	///		Implementation is based on Meziantou's 2018/02/26 article: MSTest v2 Customize test execution
	///		</comments>
	/// </credit>
	public class StaTestClassAttribute : TestClassAttribute
	{
		public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
		{
			if (testMethodAttribute is StaTestMethodAttribute)
				return testMethodAttribute;

			return new StaTestMethodAttribute(base.GetTestMethodAttribute(testMethodAttribute));
		}
	}
}
