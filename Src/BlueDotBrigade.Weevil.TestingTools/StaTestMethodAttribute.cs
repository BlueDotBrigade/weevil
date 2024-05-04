namespace BlueDotBrigade.Weevil.TestingTools
{
	using System.Threading;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <credit authors="Gérald Barré (meziantou);" href="https://www.meziantou.net/mstest-v2-customize-test-execution.htm">
	///		<license type="None" href=""/>
	///		<comments>
	///		Implementation is based on Meziantou's 2018/02/26 article: MSTest v2 Customize test execution
	///		</comments>
	/// </credit>
	public class StaTestMethodAttribute : TestMethodAttribute
	{
		private readonly TestMethodAttribute _testMethodAttribute;

		public StaTestMethodAttribute()
		{
			// nothing to do
		}

		public StaTestMethodAttribute(TestMethodAttribute testMethodAttribute)
		{
			_testMethodAttribute = testMethodAttribute;
		}

		public override TestResult[] Execute(ITestMethod testMethod)
		{
			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
				return Invoke(testMethod);

			TestResult[] result = null;
			var thread = new Thread(() => result = Invoke(testMethod));
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
			return result;
		}

		private TestResult[] Invoke(ITestMethod testMethod)
		{
			if (_testMethodAttribute != null)
				return _testMethodAttribute.Execute(testMethod);

			return new[] { testMethod.Invoke(null) };
		}
	}
}
