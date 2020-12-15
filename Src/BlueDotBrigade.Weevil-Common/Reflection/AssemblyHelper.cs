namespace BlueDotBrigade.Weevil.Reflection
{
	using System;

	public static class AssemblyHelper
	{
		/// <summary>
		///     Returns a fully qualified path for the executing assembly.
		/// </summary>
		/// <remarks>
		///     While path does include the parent directory name, it does not include the name of the assembly.
		/// </remarks>
		public static string ExecutingDirectory
		{
			get
			{
				var codeBase = AppDomain.CurrentDomain.BaseDirectory;
				var uriBuilder = new UriBuilder(new Uri(codeBase));
				var path = Uri.UnescapeDataString(uriBuilder.Path);

				return path.Replace(@"/", @"\");
			}
		}
	}
}