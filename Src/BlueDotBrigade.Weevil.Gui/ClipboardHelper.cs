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

		public static void CopyRawFromSelected(IEngine engine, bool addLineNumberPrefix)
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
						.Select(r => r.LineNumber + Delimiter + r.Content)
						.ToArray();
				}
				else
				{
					selectedItems = engine
						.Selector
						.Selected
						.Values
						.Select(r => r.Content).ToArray();
				}

				Clipboard.SetData(
					DataFormats.Text,
					string.Join(Environment.NewLine, selectedItems));
			}
		}

		public static void CopyCommentFromSelected(IEngine engine, bool addLineNumberPrefix)
		{
			IEngine coreEngine = engine;

			if (coreEngine != null)
			{
				IRecord[] selectedRecords = engine
						.Selector
						.Selected
						.Values.ToArray();

				var output = new StringBuilder();

				foreach (IRecord record in selectedRecords)
				{
					if (record.Metadata.HasComment)
					{
						var timestamp = record.HasCreationTime
							? record.CreatedAt.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture)
							: string.Empty;

						if (addLineNumberPrefix)
						{
							output.AppendLine($"{record.LineNumber}\t{timestamp}\t{record.Metadata.Comment}");
						}
						else
						{
							output.AppendLine($"{timestamp}\t{record.Metadata.Comment}");
						}
					}
				}

				if (output.Length == 0)
				{
					foreach (IRecord record in selectedRecords)
					{
						var timestamp = record.HasCreationTime
							? record.CreatedAt.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture)
							: string.Empty;

						if (addLineNumberPrefix)
						{
							output.AppendLine($"{record.LineNumber}\t{timestamp}");
						}
						else
						{
							output.AppendLine($"{timestamp}");
						}
					}
				}

				Clipboard.SetData(DataFormats.Text, output.ToString());
			}
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
