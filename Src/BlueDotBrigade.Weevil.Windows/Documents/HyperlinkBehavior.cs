namespace BlueDotBrigade.Weevil.Windows.Documents
{
	using System;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;
	using BlueDotBrigade.Weevil.Diagnostics;

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
			try
			{
				WindowsProcess.Start(WindowsProcessType.DefaultApplication, e.Uri.AbsoluteUri);
			}
			catch (Exception exception)
			{
				var url = e.Uri?.AbsoluteUri ?? string.Empty;
				
				Log.Default.Write(
					LogSeverityType.Error,
					exception,
					$"Unexpected error occurred while trying to browse to the URL. Url=`{url}`");
			}
		}
	}
}