namespace BlueDotBrigade.Weevil.TestTools.Text
{
	public class RegexHelper
	{
		public static string RevealGroups(string pattern)
		{
			if (pattern.StartsWith("(") && pattern.EndsWith(")"))
			{
				pattern = pattern.Substring(1, pattern.Length - 2);
			}
			return pattern.Replace("(?:", "(");
		}
	}
}
