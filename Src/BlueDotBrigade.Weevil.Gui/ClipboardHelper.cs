namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Text;
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

		public static void CopyRawFromSelected(IEngine engine, bool addLineNumberPrefix, IRecordFormatter formatter)
		{
			IEngine coreEngine = engine;
			if (coreEngine != null)
			{
				string[] selectedItems;

				if (addLineNumberPrefix)
				{
					selectedItems = engine
						.Selector
						.Selected
						.Values
						.Select(r => r.LineNumber + Delimiter + formatter.Format(r))
						.ToArray();
				}
				else
				{
					selectedItems = engine
						.Selector
						.Selected
						.Values
						.Select(r => formatter.Format(r)).ToArray();
				}

				Clipboard.SetData(
					DataFormats.Text,
					string.Join(Environment.NewLine, selectedItems));
			}
		}

		public static void CopySelectedComments(IEngine engine)
		{
			CopyFromSelected(engine, (record) =>
			{
				var timestamp = record.HasCreationTime
					? record.CreatedAt.ToString(ShortTimestampFormat, CultureInfo.InvariantCulture)
					: ShortMissingTimestamp;

				return record.Metadata.HasComment
					? timestamp + "\t" + record.Metadata.Comment
					: string.Empty;
			});
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

			IRecord[] selectedRecords = engine.Selector.Selected.Values.ToArray();

			var output = new StringBuilder();

			foreach (IRecord record in selectedRecords)
			{
				var textToAppend = formatter(record);

				if (string.IsNullOrEmpty(textToAppend))
				{
					// Ignore empty data. (e.g. record might not have a comment)
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
			IEngine coreEngine = engine;

			if (coreEngine != null)
			{
				System.Collections.Generic.ICollection<IRecord> selectedValues = coreEngine.Selector.Selected.Values;

				if (Clipboard.ContainsText())
				{
					if (selectedValues.Count > 0)
					{
						var newComment = Clipboard.GetText();
						foreach (IRecord record in selectedValues)
						{
							if (allowOverwrite)
							{
								record.Metadata.Comment = newComment;
							}
							else
							{
								if (string.IsNullOrWhiteSpace(record.Metadata.Comment))
								{
									record.Metadata.Comment = newComment;
								}
							}
						}
					}
				}
			}
		}
	}
}
