namespace BlueDotBrigade.Weevil.Filter
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using System.Collections.Concurrent;

	[TestClass]
	public class FilterCriteriaTest
	{
		[TestMethod]
		public void Ctor_DefaultConfiguration_IsNotNull()
		{
			// Failing to initialize static member fields properly
			// ... can result in an unexpected `null` value.
			Assert.IsNotNull(FilterCriteria.None.Configuration);
		}

		[TestMethod]
		public void NoneFilter_FilterCriteriaWithNoConfiguration_AreEqual()
		{
			var sampleCriteria = new FilterCriteria(string.Empty, string.Empty, new ConcurrentDictionary<string, object>());

			Assert.AreEqual(FilterCriteria.None, sampleCriteria);
		}
	}
}
