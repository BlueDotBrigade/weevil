namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Diagnostics;
	using System.IO;

	public sealed class DirectoryHelper
	{

		public static bool CheckIfWritable(string directoryPath)
		{
			var isWritable = false;

			var filePath = Path.Combine(directoryPath, Path.GetRandomFileName());
			try
			{
				using (FileStream fs = System.IO.File.Create(filePath, 1, FileOptions.DeleteOnClose))
				{
					isWritable = true;
				}
			}
			catch (UnauthorizedAccessException e)
			{
				Debug.WriteLine(e.Message);
			}

			return isWritable;
		}
	}
}
