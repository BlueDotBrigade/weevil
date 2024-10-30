namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Represents commonly used parameters in Reqnroll <see href="https://docs.reqnroll.net/latest/automation/step-definitions.html">step definitions</see>.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/step-definitions.html">Reqnroll: Step Definitions</seealso>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/cucumber-expressions.html">Reqnroll: Gherkin Expressions</seealso>
	/// <seealso href="https://regex101.com">Regular Expressions</seealso>
	internal sealed class X
	{
		public const string FileName = @"""([a-zA-Z0-9]+\.[a-zA-Z0-9]{1,4})""";

		public const string FilePath = @"((?:(?<Drv>[A-Za-z]:\\))(?<Dir>([a-zA-Z0-9._-]+\\)*)(?<File>[a-zA-Z0-9._-]+)\.(?<Ext>[a-zA-Z]{1,4}))";

		//public const string TextExpression = @"(plain text|regular expression)"; <<< The Renroll Gherkin statement does not recognize this at compile time
		public const string TextExpression ="(.*)";

		public const string WholeNumber = @"(\d+)";

		public const string AnyText = @"(.*)";

		public const string RecordCount = @"(\d+)";

		public const string OnOff = @"(on|off)";

		public const string ShowSeverityOption = @"(Show Debug|Show Trace)";

		public const string FilterMode = @"(Plain Text|Regular Expression)";
	}
}