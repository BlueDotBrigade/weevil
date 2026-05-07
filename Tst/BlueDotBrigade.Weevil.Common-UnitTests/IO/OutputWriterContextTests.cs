namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Diagnostics;
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

		[TestMethod]
		public void GivenNumberingResetOperation_WhenWriteSubHeadingThenWriteNumberedCalled_ThenNumberingRestartsFromOne()
		{
			var formatter = new TrackingFormatter();
			var writer = new RecordingWriter();

			OutputWriterContext.Configure(formatter, writer);

			OutputWriterContext.WriteNumbered("first");
			OutputWriterContext.WriteSubHeading("sub-heading");
			OutputWriterContext.WriteNumbered("after-sub-heading");

			CollectionAssert.AreEqual(
				new[] { "N1:first", "SH:sub-heading", "N1:after-sub-heading" },
				writer.Messages.ToArray());
		}

		[TestMethod]
		public void GivenTableHeaderOperation_WhenWriteTableHeaderThenWriteNumberedCalled_ThenNumberingRestartsFromOne()
		{
			var formatter = new TrackingFormatter();
			var writer = new RecordingWriter();

			OutputWriterContext.Configure(formatter, writer);

			OutputWriterContext.WriteNumbered("first");
			OutputWriterContext.WriteTableHeader(new[] { "A" });
			OutputWriterContext.WriteNumbered("after-table-header");

			CollectionAssert.AreEqual(
				new[] { "N1:first", "TH:A", "N1:after-table-header" },
				writer.Messages.ToArray());
		}

		[TestMethod]
		public void GivenNumberingResetOperation_WhenWriteWarningThenWriteNumberedCalled_ThenNumberingRestartsFromOne()
		{
			var formatter = new TrackingFormatter();
			var writer = new RecordingWriter();

			OutputWriterContext.Configure(formatter, writer);

			OutputWriterContext.WriteNumbered("first");
			OutputWriterContext.WriteWarning("warning");
			OutputWriterContext.WriteNumbered("after-warning");

			CollectionAssert.AreEqual(
				new[] { "N1:first", "N1:after-warning" },
				writer.Messages.ToArray());
		}

		[TestMethod]
		public void GivenWarningMessage_WhenWriteWarningCalled_ThenWarningIsLoggedAsDiagnostic()
		{
			// Regression: Issue #836
			var previous = Log.Default;
			var logWriter = new RecordingLogWriter();
			Log.Register(logWriter);

			try
			{
				OutputWriterContext.WriteWarning("warning");

				CollectionAssert.AreEqual(new[] { "Warning:warning" }, logWriter.Messages.ToArray());
			}
			finally
			{
				Log.Register(previous);
			}
		}

		[TestMethod]
		public void GivenErrorMessage_WhenWriteErrorCalled_ThenErrorIsLoggedAsDiagnostic()
		{
			// Regression: Issue #836
			var previous = Log.Default;
			var logWriter = new RecordingLogWriter();
			Log.Register(logWriter);

			try
			{
				OutputWriterContext.WriteError("error");

				CollectionAssert.AreEqual(new[] { "Error:error" }, logWriter.Messages.ToArray());
			}
			finally
			{
				Log.Register(previous);
			}
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
			public string AsSubHeading(string message) => $"SH:{message}";
			public string AsBullet(string message) => $"B:{message}";
			public string AsNumbered(string message) => $"N{_counter++}:{message}";
			public string AsTableHeader(string[] headers) => $"TH:{string.Join(",", headers)}";
			public string AsTableRow(string[] columns) => $"TR:{string.Join(",", columns)}";
			public string AsTable(string[] headers, string[][] rows) => string.Empty;

			public void ResetNumbering()
			{
				_counter = 1;
			}
		}

		private sealed class RecordingLogWriter : ILogWriter
		{
			public List<string> Messages { get; } = new();

			public void Write(string message)
			{
			}

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
			}

			public void Write(LogSeverityType severity, string message)
			{
				Messages.Add($"{severity}:{message}");
			}

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				Messages.Add($"{severity}:{message}");
			}

			public void Write(LogSeverityType severity, Exception exception)
			{
			}

			public void Write(LogSeverityType severity, Exception exception, string message)
			{
			}

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
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
