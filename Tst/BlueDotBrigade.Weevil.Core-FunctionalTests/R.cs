namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Represents commonly used parameters in Reqnroll <see href="https://docs.reqnroll.net/latest/automation/step-definitions.html">step definitions</see>.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/step-definitions.html">Reqnroll: Step Definitions</seealso>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/cucumber-expressions.html">Reqnroll: Gherkin Expressions</seealso>
	/// <seealso href="https://regex101.com">Regular Expressions</seealso>
	internal sealed class R
	{
		public const string FileName = @"(.*)";

		public const string FilePath = @"(.*)";

		//public const string TextExpression = @"(plain text|regular expression)"; <<< The Renroll Gherkin statement does not recognize this at compile time
		public const string TextExpression = @"(.*)";

		public const string AnyText = @"(.*)";

		public const string RecordCount = @"(\d+)";	
	}
}