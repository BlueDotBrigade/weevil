namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using System;
	using System.Runtime.InteropServices;

	internal static class KnownFolders
	{
		private static readonly Guid DownloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");

		/// <summary>
		/// The Downloads folder, which <see cref="Environment.SpecialFolder"/> lacks, or <see langword="null"/>.
		/// </summary>
		public static string GetDownloads()
		{
			if (!OperatingSystem.IsWindows())
			{
				return null;
			}

			if (SHGetKnownFolderPath(DownloadsId, 0, IntPtr.Zero, out var path) != 0)
			{
				return null;
			}

			try
			{
				return Marshal.PtrToStringUni(path);
			}
			finally
			{
				Marshal.FreeCoTaskMem(path);
			}
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid folderId, uint flags, IntPtr token, out IntPtr path);
	}
}
