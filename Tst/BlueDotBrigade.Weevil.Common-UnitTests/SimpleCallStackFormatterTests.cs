namespace BlueDotBrigade.Weevil.Common
{
	using System;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SimpleCallStackFormatterTests
	{
		[TestMethod]
		public void Format_SimpleCallStack_ReturnsContentWithoutSystemNamespaces()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Debug, new Daten().AsString());
			record.Metadata.IsMultiLine = true;

			var actualResult = new SimpleCallStackFormatter().Format(record);

			Assert.AreEqual<string>(
				"Debug 2021-15-21 12:59:59 AcmeAssembly.dll Something bad happened. System.ObjectDisposedException: Cannot access a disposed object.\r\n" +
				"   at Company.Product.Component.DataCollector.Fetch()",
				actualResult);
		}

		[TestMethod]
		public void Format_SimpleCallStack_ReturnsTrue()
		{
			var originalContent = new Daten().AsString();
			var record = new Record(1, DateTime.Now, SeverityType.Debug, originalContent);
			record.Metadata.IsMultiLine = true;

			var formattedResult = new SimpleCallStackFormatter().Format(record);

			Assert.IsTrue(originalContent.Length > formattedResult.Length);
		}

		[TestMethod]
		public void Format_NoCallStack_ReturnsFalse()
		{
			var originalContent = "The quick brown fox jumps over the lazy dog.";
			var record = new Record(1, DateTime.Now, SeverityType.Debug, originalContent);
			record.Metadata.IsMultiLine = true;

			var formattedResult = new SimpleCallStackFormatter().Format(record);

			Assert.IsTrue(originalContent.Length == formattedResult.Length);
		}

		[TestMethod]
		public void Format_CallStackWithPaths_ReturnsCallStackWithoutPaths()
		{
			var originalContent = new Daten().AsString();
			var record = new Record(1, DateTime.Now, SeverityType.Debug, originalContent);
			record.Metadata.IsMultiLine = true;

			var formattedResult = new SimpleCallStackFormatter().Format(record);

			Assert.IsTrue(originalContent.Length > formattedResult.Length);
		}
	}
}
