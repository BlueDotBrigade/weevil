namespace BlueDotBrigade.Weevil.Filter
{
	using System.Collections.Immutable;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FilteringShould
	{
		[TestMethod]
		public void CountNumberOfInformationalMessages()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			var actualErrors = int.Parse(engine.Filter.GetMetrics()["Information"].ToString());
			Assert.AreEqual(512, actualErrors);
		}

		[TestMethod]
		public void SupportFindingRecordsWithComments()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			ImmutableArray<IRecord> results = engine
				.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("@Comment"))
				.Results;

			Assert.AreEqual(2, results.Length);
			Assert.IsTrue(results[0].Metadata.HasComment);
			Assert.AreEqual(1, results[0].LineNumber);
			Assert.IsTrue(results[1].Metadata.HasComment);
			Assert.AreEqual(512, results[1].LineNumber);
		}

		[TestMethod]
		public void SupportFindingSpecificComment()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			ImmutableArray<IRecord> results = engine
				.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("@Comment=Last"))
				.Results;

			Assert.AreEqual(1, results.Length);
			Assert.AreEqual(512, results[0].LineNumber);
			Assert.IsTrue(results[0].Metadata.HasComment);
		}
	}
}
