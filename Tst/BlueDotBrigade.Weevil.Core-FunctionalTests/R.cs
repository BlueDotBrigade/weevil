namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Represents commonly used parameters in SpecFlow <see href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Step-Definitions.html">step definitions</see>.
	/// </summary>
	/// <seealso href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Step-Definitions.html">SpecFlow: Step Definitions</seealso>
	/// <seealso href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Cucumber-Expressions.html">Gherkin Expressions</seealso>
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