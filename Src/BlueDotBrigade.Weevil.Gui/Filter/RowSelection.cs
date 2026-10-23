namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Windows.Input;

	/// <summary>
	/// Turns clicks, the selection keys and navigation into changes to Core's selection, so WPF never selects a row (#1064).
	/// </summary>
	/// <remarks>
	/// Core keeps the selection and the anchor. The caret, the row with keyboard focus, is kept here as a line number, so it
	/// survives a filter: when the filter hides it, the next record shown takes its place.
	/// Dragging the mouse selects nothing more than the press does (decided 2026-10-23).
	/// </remarks>
	internal sealed class RowSelection
	{
		private const int NoLineNumber = -1;

		private readonly IEngine _engine;

		public RowSelection(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public int CaretLineNumber { get; private set; } = NoLineNumber;

		private ISelect Selector => _engine.Selector;

		/// <summary>
		/// Returns the caret's position: its record, or the next one shown when the filter hides it; -1 when there is no caret.
		/// </summary>
		public int GetCaretPosition(RowList rows)
		{
			return this.CaretLineNumber == NoLineNumber || rows.Count == 0
				? -1
				: System.Math.Min(rows.IndexOfLineNumberOrNext(this.CaretLineNumber), rows.Count - 1);
		}

		/// <summary>
		/// A left click on the row at <paramref name="position"/>, as Weevil 2.12 handled it.
		/// </summary>
		public void Click(RowList rows, int position, ModifierKeys modifiers)
		{
			var lineNumber = rows.LineNumberAt(position);

			if (modifiers.HasFlag(ModifierKeys.Shift))
			{
				ExtendTo(lineNumber, modifiers.HasFlag(ModifierKeys.Control), lineNumber);
			}
			else if (modifiers.HasFlag(ModifierKeys.Control))
			{
				this.Selector.Toggle(lineNumber);
			}
			else
			{
				this.Selector.SelectOnly(lineNumber, lineNumber);
			}

			this.CaretLineNumber = lineNumber;
		}

		/// <summary>
		/// A right click selects only its row, unless the row is already selected, so the context menu acts on what was clicked.
		/// </summary>
		public void RightClick(RowList rows, int position, ModifierKeys modifiers)
		{
			var lineNumber = rows.LineNumberAt(position);

			if ((modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None &&
				!this.Selector.Selected.ContainsKey(lineNumber))
			{
				this.Selector.SelectOnly(lineNumber, lineNumber);
			}

			this.CaretLineNumber = lineNumber;
		}

		/// <summary>
		/// Moves only the caret, for example to the row whose pin box was clicked. The selection does not change.
		/// </summary>
		public void MoveCaretTo(RowList rows, int position)
		{
			this.CaretLineNumber = rows.LineNumberAt(position);
		}

		/// <summary>
		/// Selects only the row a navigation command found, such as Find Next or Go To.
		/// </summary>
		public void SelectOnly(RowList rows, int position)
		{
			var lineNumber = rows.LineNumberAt(position);

			this.Selector.SelectOnly(lineNumber, lineNumber);
			this.CaretLineNumber = lineNumber;
		}

		/// <summary>
		/// Handles a selection key.
		/// </summary>
		/// <param name="pageSize">The number of rows Page Up and Page Down move.</param>
		/// <param name="isOnRow">The key was pressed on a row, not on a control inside it, so Space selects.</param>
		/// <param name="scrollToPosition">The row to scroll to and focus, or -1.</param>
		/// <returns><see langword="false"/> when the key is not a selection key, so other handlers get it. A letter is not one.</returns>
		public bool TryPressKey(RowList rows, Key key, ModifierKeys modifiers, int pageSize, bool isOnRow, out int scrollToPosition)
		{
			scrollToPosition = -1;

			// Alt+Space opens the window menu, and Alt+arrow belongs to others, as in WPF's own list.
			if (rows.Count == 0 || (modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == ModifierKeys.Alt)
			{
				return false;
			}

			var caret = GetCaretPosition(rows);

			switch (key)
			{
				case Key.Up:
					scrollToPosition = MoveCaret(rows, caret - 1, modifiers);
					return true;
				case Key.Down:
					scrollToPosition = MoveCaret(rows, caret + 1, modifiers);
					return true;
				case Key.PageUp:
					scrollToPosition = MoveCaret(rows, caret - pageSize, modifiers);
					return true;
				case Key.PageDown:
					scrollToPosition = MoveCaret(rows, caret + pageSize, modifiers);
					return true;
				case Key.Home:
					scrollToPosition = MoveCaret(rows, 0, modifiers);
					return true;
				case Key.End:
					scrollToPosition = MoveCaret(rows, rows.Count - 1, modifiers);
					return true;
				case Key.A when modifiers == ModifierKeys.Control:
					this.Selector.SelectAll();
					return true;
				case Key.Space when isOnRow && caret >= 0:
					SelectCaret(rows.LineNumberAt(caret), modifiers);
					return true;
				default:
					return false;
			}
		}

		/// <remarks>
		/// Ctrl alone moves only the caret. Shift selects from the anchor; with Ctrl it adds to the selection (decided 2026-10-23).
		/// </remarks>
		private int MoveCaret(RowList rows, int position, ModifierKeys modifiers)
		{
			position = System.Math.Clamp(position, 0, rows.Count - 1);
			var lineNumber = rows.LineNumberAt(position);

			if (modifiers.HasFlag(ModifierKeys.Shift))
			{
				ExtendTo(lineNumber, modifiers.HasFlag(ModifierKeys.Control), lineNumber);
			}
			else if (!modifiers.HasFlag(ModifierKeys.Control))
			{
				this.Selector.SelectOnly(lineNumber, lineNumber);
			}

			this.CaretLineNumber = lineNumber;

			return position;
		}

		private void SelectCaret(int lineNumber, ModifierKeys modifiers)
		{
			if (modifiers.HasFlag(ModifierKeys.Shift))
			{
				ExtendTo(lineNumber, modifiers.HasFlag(ModifierKeys.Control), lineNumber);
			}
			else if (modifiers.HasFlag(ModifierKeys.Control))
			{
				this.Selector.Toggle(lineNumber);
			}
			else
			{
				this.Selector.SelectOnly(lineNumber, lineNumber);
			}

			this.CaretLineNumber = lineNumber;
		}

		/// <param name="anchorWhenNone">The anchor to use when Core has none, for example after a filter hid it, and there is no caret either.</param>
		/// <remarks>
		/// Called before the caret moves, so a missing anchor falls back to where the caret was.
		/// </remarks>
		private void ExtendTo(int lineNumber, bool addsToSelection, int anchorWhenNone)
		{
			var anchorLineNumber = this.Selector.AnchorLineNumber != NoLineNumber
				? this.Selector.AnchorLineNumber
				: this.CaretLineNumber != NoLineNumber ? this.CaretLineNumber : anchorWhenNone;

			if (addsToSelection)
			{
				this.Selector.SelectRange(anchorLineNumber, lineNumber);
			}
			else
			{
				this.Selector.SelectOnly(anchorLineNumber, lineNumber);
			}
		}
	}
}
