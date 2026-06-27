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
			FilterCriteria.None.Configuration.Should().NotBeNull();
		}

		[TestMethod]
		public void NoneFilter_FilterCriteriaWithNoConfiguration_AreEqual()
		{
			var sampleCriteria = new FilterCriteria(string.Empty, string.Empty, new ConcurrentDictionary<string, object>());

			sampleCriteria.Should().Be(FilterCriteria.None);
		}
	}
}
