namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	public static class Log
	{
		private static ILogWriter _defaultWriter = new DefaultLogWriter();

		public static ILogWriter Default => _defaultWriter;

		public static void Register(ILogWriter writer)
		{
			_defaultWriter = writer ?? throw new ArgumentNullException(nameof(writer));
		}
	}
}