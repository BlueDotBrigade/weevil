namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// What the analyst accepts by opening a log, which the Open As dialog shows just above Open (#1216).
	/// </summary>
	/// <remarks>
	/// The data context is the license file's path.
	/// </remarks>
	public partial class OpenAsNoticeView : UserControl
	{
		public OpenAsNoticeView()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Opens the license with the application that opens its kind of file, or else with Notepad, since Windows may have none for <c>.md</c> files.
		/// </summary>
		private void OnLicenseClick(object sender, RoutedEventArgs e)
		{
			var licensePath = this.DataContext as string;

			try
			{
				WindowsProcess.Start(WindowsProcessType.DefaultApplication, licensePath);
			}
			catch (Exception defaultFailure)
			{
				Log.Default.Write(LogSeverityType.Information, defaultFailure, $"No application opened the license, so Notepad opens it. Path={licensePath}");

				try
				{
					Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { licensePath }, UseShellExecute = false });
				}
				catch (Exception notepadFailure)
				{
					Log.Default.Write(LogSeverityType.Error, notepadFailure, $"The license could not be opened. Path={licensePath}");
				}
			}
		}
	}
}
