namespace BlueDotBrigade.Weevil.Windows.Documents
{
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;

	// https://stackoverflow.com/a/11433814/949681
	public static class HyperlinkBehavior
	{
		public static bool GetOpenInBrowser(DependencyObject obj)
		{
			return (bool)obj.GetValue(OpenInBrowserProperty);
		}

		public static void SetOpenInBrowser(DependencyObject obj, bool value)
		{
			obj.SetValue(OpenInBrowserProperty, value);
		}
		public static readonly DependencyProperty OpenInBrowserProperty =
			DependencyProperty.RegisterAttached("OpenInBrowser", typeof(bool), typeof(HyperlinkBehavior), new UIPropertyMetadata(false, OnIsExternalChanged));

		private static void OnIsExternalChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			var hyperlink = sender as Hyperlink;

			if ((bool)args.NewValue)
			{
				hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
			}
			else
			{
				hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
			}
		}

		private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}
}
