namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Windows;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Data;

	public static class ClipboardHelper
	{
		private const string Delimiter = " === ";

		private const string ShortTimestampFormat =  @"HH:mm:ss.ffff";
		private const string ShortMissingTimestamp = @"--:--:--.----";
		
		private const string LongTimestampFormat =  @"yyyy-MM-dd HH:mm:ss.ffff";
		private const string LongMissingTimestamp = @"---------- --:--:--.----";

		/// <summary>
		/// Returns the text Copy puts on the clipboard for <paramref name="records"/>: one record per line, each formatted by <paramref name="formatter"/>.
		/// </summary>
		/// <remarks>
		/// Reads each record's text, so on disk it is given records that hold their text, copied in the background (#1335).
		/// A formatter can be slow, such as the simple call stack's regular expressions on 100,000 records, so the token is checked before each record.
		/// </remarks>
		/// <param name="addLineNumberPrefix">Each line starts with the record's line number.</param>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		public static string FormatRaw(RecordList records, bool addLineNumberPrefix, IRecordFormatter formatter, CancellationToken cancellationToken = default)
		{
			if (records == null) throw new ArgumentNullException(nameof(records));
			if (formatter == null) throw new ArgumentNullException(nameof(formatter));

			IEnumerable<string> lines = records.Select(record =>
			{
				cancellationToken.ThrowIfCancellationRequested();

				var text = formatter.Format(record);

				return addLineNumberPrefix ? record.LineNumber + Delimiter + text : text;
			});

			return string.Join(Environment.NewLine, lines);
		}

		/// <summary>
		/// Puts <paramref name="text"/> on the clipboard. Called on the UI thread.
		/// </summary>
		public static void SetText(string text)
		{
			Clipboard.SetData(DataFormats.Text, text);
		}

		public static void CopySelectedComments(IEngine engine)
		{
			if (engine == null) throw new ArgumentNullException(nameof(engine));

			RecordList selected = engine.Selector.GetSelected();
			var output = new StringBuilder();

			// Walks the selected line numbers, so only a record with a comment is created (#1139).
			for (var position = 0; position < selected.Count; position++)
			{
				var lineNumber = selected.GetLineNumber(position);

				if (engine.Annotations.HasComment(lineNumber))
				{
					IRecord record = selected[position];

					var timestamp = record.HasCreationTime
						? record.CreatedAt.ToString(ShortTimestampFormat, CultureInfo.InvariantCulture)
						: ShortMissingTimestamp;

					output.AppendLine(timestamp + "\t" + engine.Annotations.GetComment(lineNumber));
				}
			}

			Clipboard.SetData(DataFormats.Text, output.ToString().TrimEnd());
		}
		public static void CopySelectedLineNumbers(IEngine engine)
		{
			CopyFromSelected(engine, (record) => record.LineNumber.ToString("#,###,##0"));
		}

		public static void CopySelectedTimestamps(IEngine engine)
		{
			CopyFromSelected(engine, (record) =>
			{
				return record.HasCreationTime
					? record.CreatedAt.ToString(LongTimestampFormat, CultureInfo.InvariantCulture)
					: LongMissingTimestamp;
			});
		}

		public static void CopyFromSelected(IEngine engine, Func<IRecord, string> formatter)
		{
			if (engine == null) throw new ArgumentNullException(nameof(engine));
			if (formatter == null) throw new ArgumentNullException(nameof(formatter));

			RecordList selectedRecords = engine.Selector.GetSelected();

			var output = new StringBuilder();

			foreach (IRecord record in selectedRecords)
			{
				var textToAppend = formatter(record);

				if (string.IsNullOrEmpty(textToAppend))
				{
					// Ignore empty data.
				}
				else
				{
					output.AppendLine(textToAppend);
				}
			}

			Clipboard.SetData(DataFormats.Text, output.ToString().TrimEnd());
		}

		internal static void PasteToSelected(IEngine engine, bool allowOverwrite = false)
		{
			if (engine != null && engine.Selector.SelectedCount > 0 && Clipboard.ContainsText())
			{
				engine.Selector.SetComment(Clipboard.GetText(), allowOverwrite);
			}
		}
	}
}
