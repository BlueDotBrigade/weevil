﻿namespace BlueDotBrigade.Weevil.Gui
{
	/// <summary>
	/// Represents commonly used parameters in Reqnroll <see href="https://docs.reqnroll.net/latest/automation/step-definitions.html">step definitions</see>.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/step-definitions.html">Reqnroll: Step Definitions</seealso>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/cucumber-expressions.html">Reqnroll: Gherkin Expressions</seealso>
	/// <seealso href="https://regex101.com">Regular Expressions</seealso>
	internal sealed class X
	{
		public const string AnyText = @"(.*)";

		public const string Integer = @"(\d+)";

		public const string FileName = @"""([a-zA-Z0-9]+\.[a-zA-Z0-9]{1,4})""";

		public const string TimePeriod = @"((?:\d+) (?:min|sec))";
	}
}

