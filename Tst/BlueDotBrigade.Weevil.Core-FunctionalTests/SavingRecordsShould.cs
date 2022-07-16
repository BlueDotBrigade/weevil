namespace BlueDotBrigade.Weevil
{
	using System.IO;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Analysis;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SavingRecordsShould
	{
		[TestMethod]
		public void SaveAllRecordsWhenOnlyOneSelected()
		{
			var originalPath = InputData.GetFilePath("GenericBaseline.log");
			var newFilePath = Path.Combine(Path.GetTempPath(), $"{nameof(SaveAllRecordsWhenOnlyOneSelected)}.log");

			IEngine originalEngine = Engine
				.UsingPath(originalPath)
				.Open();

			originalEngine.Selector.Select(lineNumber: 8);

			try
			{
				originalEngine.Selector.SaveSelection(newFilePath, FileFormatType.Raw);

				IEngine newEngine = Engine
					.UsingPath(newFilePath)
					.Open();

				Assert.AreEqual(originalEngine.Count, newEngine.Count);
			}
			finally
			{
				if (File.Exists(newFilePath))
				{
					File.Delete(newFilePath);
				}
			}
		}
	}
}
