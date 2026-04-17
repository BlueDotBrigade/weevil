namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	[DoNotParallelize]
	public class OutputWriterContextTests
	{
		[TestMethod]
		public void GivenNullFormatter_WhenConfigureCalled_ThenThrowsArgumentNullException()
		{
			ExpectThrows<ArgumentNullException>(() => OutputWriterContext.Configure(null!, new RecordingWriter()));
		}

		[TestMethod]
		public void GivenNullWriter_WhenConfigureCalled_ThenThrowsArgumentNullException()
		{
			ExpectThrows<ArgumentNullException>(() => OutputWriterContext.Configure(new TrackingFormatter(), null!));
		}

		[TestMethod]
		public void GivenNumberingResetOperation_WhenWriteTextThenWriteNumberedCalled_ThenNumberingRestartsFromOne()
		{
			var formatter = new TrackingFormatter();
			var writer = new RecordingWriter();

			OutputWriterContext.Configure(formatter, writer);

			OutputWriterContext.WriteNumbered("first");
			OutputWriterContext.WriteNumbered("second");
			OutputWriterContext.WriteText("reset");
			OutputWriterContext.WriteNumbered("after-reset");

			CollectionAssert.AreEqual(
				new[] { "N1:first", "N2:second", "T:reset", "N1:after-reset" },
				writer.Messages.ToArray());
		}

		private sealed class RecordingWriter : IOutputWriter
		{
			public List<string> Messages { get; } = new();

			public void WriteLine(string message)
			{
				Messages.Add(message);
			}
		}

		private sealed class TrackingFormatter : IOutputFormatter
		{
			private int _counter = 1;

			public string AsText(string message) => $"T:{message}";
			public string AsHeading(string message) => $"H:{message}";
			public string AsBullet(string message) => $"B:{message}";
			public string AsNumbered(string message) => $"N{_counter++}:{message}";
			public string AsError(string message) => $"E:{message}";
			public string AsTable(string[] headers, string[][] rows) => string.Empty;

			public void ResetNumbering()
			{
				_counter = 1;
			}
		}

		private static void ExpectThrows<TException>(Action action) where TException : Exception
		{
			try
			{
				action();
			}
			catch (TException)
			{
				return;
			}

			Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
		}
	}
}
