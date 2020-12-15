namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;

	internal static class UriHelper
	{
		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		public static Uri EnsureAbsolute(Uri value)
		{
			if (!value.IsFile)
			{
				throw new InvalidOperationException("The Uri was expected to reference a file.");
			}

			Uri result = value;

			if (value.AbsolutePath.StartsWith(@"/"))
			{
				var absolutePath = Path.Combine(EnvironmentHelper.GetExecutableDirectory(), value.AbsolutePath.Substring(1));
				result = new Uri(absolutePath);
			}

			return result;
		}
	}
}
