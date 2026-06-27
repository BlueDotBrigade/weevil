namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Documents;

	/// <summary>
	/// Provides an attached property that renders a detection pattern string into a
	/// <see cref="TextBlock"/> with mixed inline formatting.
	/// </summary>
	/// <remarks>
	/// Characters enclosed in square brackets (e.g. <c>[1]</c>) are rendered bold and underlined
	/// to indicate records that will be flagged by the corresponding analysis tool.
	/// All other characters are rendered in normal weight without decoration.
	/// Example pattern: <c>[1]xx[2]xx</c> renders "1" and "2" as flagged, "x" as plain.
	/// <para>
	/// Alternative visual styles that could be used to indicate flagged records:
	/// <list type="bullet">
	/// <item><description>Bold + italic (no underline, avoids hotkey ambiguity)</description></item>
	/// <item><description>Bold only (minimal but accessible)</description></item>
	/// <item><description>Bold + overline (less commonly associated with hotkeys than underline)</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	internal static class FormattedPatternBehavior
	{
		public static readonly DependencyProperty PatternProperty =
			DependencyProperty.RegisterAttached(
				"Pattern",
				typeof(string),
				typeof(FormattedPatternBehavior),
				new PropertyMetadata(null, OnPatternChanged));

		public static string GetPattern(DependencyObject obj)
		{
			return (string)obj.GetValue(PatternProperty);
		}

		public static void SetPattern(DependencyObject obj, string value)
		{
			obj.SetValue(PatternProperty, value);
		}

		private static void OnPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not TextBlock textBlock)
			{
				return;
			}

			textBlock.Inlines.Clear();

			var pattern = e.NewValue as string;

			if (string.IsNullOrEmpty(pattern))
			{
				return;
			}

			var position = 0;
			while (position < pattern.Length)
			{
				if (pattern[position] == '[')
				{
					var closingBracket = pattern.IndexOf(']', position + 1);
					if (closingBracket > position + 1)
					{
						var flaggedText = pattern.Substring(position + 1, closingBracket - position - 1);
						var run = new Run(flaggedText)
						{
							FontWeight = FontWeights.Bold,
							TextDecorations = TextDecorations.Underline,
						};
						textBlock.Inlines.Add(run);
						position = closingBracket + 1;
						continue;
					}
				}

				textBlock.Inlines.Add(new Run(pattern[position].ToString()));
				position++;
			}
		}
	}
}
