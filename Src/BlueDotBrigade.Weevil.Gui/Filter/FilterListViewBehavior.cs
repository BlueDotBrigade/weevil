namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Threading;

	/// <summary>
	/// Provides attached behaviors specific to the filter results <see cref="ListView"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>ActiveRecordIndex</c> attached property is the View's signal that the View
	/// Model wants the user's attention on a specific record (e.g. after "Next Comment",
	/// "Next Pin", or any navigation command). The flow is intentionally one-way:
	/// View Model → View. The XAML binding therefore uses <c>Mode=OneWay</c>.
	/// </para>
	/// <para>
	/// Issue #806 — do not reintroduce a <see cref="Selector.SelectionChanged"/> handler
	/// here that writes <see cref="Selector.SelectedIndex"/> back into
	/// <c>ActiveRecordIndex</c>. When <c>Ctrl+Shift</c> is held during navigation, WPF
	/// range-extends the selection while <see cref="UIElement.Focus"/> runs, so
	/// <c>SelectedIndex</c> points at the range anchor — not the navigation target. A
	/// back-write would leave <see cref="FilterViewModel.ActiveRecordIndex"/> stale, and
	/// Metalama <c>[Observable]</c> would then silently suppress the next "wrap-to-self"
	/// assignment because the new value matches the back-written anchor. See:
	/// <c>Doc/Notes/Design/Issue806-CommentNavigationBug.md</c>.
	/// </para>
	/// </remarks>
	public class FilterListViewBehavior : DependencyObject
	{
		public static int GetActiveRecordIndex(ListView listView)
		{
			return (int)listView.GetValue(ActiveRecordIndexProperty);
		}

		public static void SetActiveRecordIndex(ListView listView, int value)
		{
			listView.SetValue(ActiveRecordIndexProperty, value);
		}

		public static readonly DependencyProperty ActiveRecordIndexProperty =
			DependencyProperty.RegisterAttached(
				"ActiveRecordIndex",
				typeof(int),
				typeof(FilterListViewBehavior),
				new FrameworkPropertyMetadata(
					-1,
					FrameworkPropertyMetadataOptions.None,
					OnActiveRecordIndexChanged));

		internal static bool HasVisibleItemAtIndex(int activeRecordIndex, int itemCount)
		{
			return activeRecordIndex >= 0 && activeRecordIndex < itemCount;
		}

		internal static bool ShouldNormalizeSelection(
			int selectedItemCount,
			int selectedIndex,
			int activeRecordIndex)
		{
			return selectedItemCount != 1 || selectedIndex != activeRecordIndex;
		}

		private static void OnActiveRecordIndexChanged(
			DependencyObject d,
			DependencyPropertyChangedEventArgs e)
		{
			if (d is not ListView listView || e.NewValue is not int activeRecordIndex)
			{
				return;
			}

			if (!HasVisibleItemAtIndex(activeRecordIndex, listView.Items.Count))
			{
				return;
			}

			listView.ScrollIntoView(listView.Items[activeRecordIndex]);
			listView.Dispatcher.BeginInvoke(
				DispatcherPriority.Loaded,
				new Action(() => SynchronizeSelection(listView, activeRecordIndex)));
		}

		private static void SynchronizeSelection(ListView listView, int activeRecordIndex)
		{
			if (!HasVisibleItemAtIndex(activeRecordIndex, listView.Items.Count) ||
				listView.ItemContainerGenerator.ContainerFromIndex(activeRecordIndex) is not ListViewItem item)
			{
				return;
			}

			item.Focus();

			if (!ShouldNormalizeSelection(
				listView.SelectedItems.Count,
				listView.SelectedIndex,
				activeRecordIndex))
			{
				return;
			}

			// Collapse a range-extended selection (e.g. when Ctrl+Shift is held during
			// navigation) back to the single navigation target. Issue #768.
			listView.SelectedItems.Clear();
			listView.SelectedItems.Add(listView.Items[activeRecordIndex]);
		}
	}

}