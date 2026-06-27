namespace BlueDotBrigade.Weevil
{
	using System.IO;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Test;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SavingRecordsShould
	{
		[TestMethod]
		public void SaveAllRecordsWhenOnlyOneSelected()
		{
			var originalPath = new Daten().AsFilePath(From.GlobalDefault);
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

				newEngine.Count.Should().Be(originalEngine.Count);
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
			var originalPath = new Daten().AsFilePath(From.GlobalDefault);
			var newPath = Path.Combine(Path.GetTempPath(), $"{nameof(SaveWithSameEncodingAsSourceFile)}.log");

			IEngine originalEngine = Engine
				.UsingPath(originalPath)
				.Open();

			originalEngine.Selector.Select(lineNumber: 8);

			try
			{
				originalEngine.Selector.SaveSelection(newPath, FileFormatType.Raw);

				(EncodingHelper.GetEncoding(newPath)).Should().Be(EncodingHelper.GetEncoding(originalPath));
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
