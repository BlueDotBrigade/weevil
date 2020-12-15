namespace BlueDotBrigade.Weevil
{
	using System.IO;
	using System.Reflection;

	public static class EnvironmentHelper
	{
		public static string GetExecutableDirectory()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
	}
}
