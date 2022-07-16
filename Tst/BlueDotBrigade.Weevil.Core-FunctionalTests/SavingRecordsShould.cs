namespace BlueDotBrigade.Weevil
{
	using System.IO;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Test;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SavingRecordsShould
	{
		[TestMethod]
		public void SaveAllRecordsWhenOnlyOneSelected()
		{
			var originalPath = InputData.GetFilePath("GenericBaseline.log");
			var newPath = Path.Combine(Path.GetTempPath(), $"{nameof(SaveAllRecordsWhenOnlyOneSelected)}.log");

			IEngine originalEngine = Engine
				.UsingPath(originalPath)
				.Open();

			originalEngine.Selector.Select(lineNumber: 8);

			try
			{
				originalEngine.Selector.SaveSelection(newPath, FileFormatType.Raw);

				IEngine newEngine = Engine
					.UsingPath(newPath)
					.Open();

				Assert.AreEqual(originalEngine.Count, newEngine.Count);
			}
			finally
			{
				if (File.Exists(newPath))
				{
					File.Delete(newPath);
				}
			}
		}

		[TestMethod]
		public void SaveWithSameEncodingAsSourceFile()
		{
			var originalPath = InputData.GetFilePath("GenericBaseline.log");
			var newPath = Path.Combine(Path.GetTempPath(), $"{nameof(SaveWithSameEncodingAsSourceFile)}.log");

			IEngine originalEngine = Engine
				.UsingPath(originalPath)
				.Open();

			originalEngine.Selector.Select(lineNumber: 8);

			try
			{
				originalEngine.Selector.SaveSelection(newPath, FileFormatType.Raw);

				Assert.AreEqual(
					EncodingHelper.GetEncoding(originalPath), 
					EncodingHelper.GetEncoding(newPath));
			}
			finally
			{
				if (File.Exists(newPath))
				{
					File.Delete(newPath);
				}
			}
		}
	}
}
