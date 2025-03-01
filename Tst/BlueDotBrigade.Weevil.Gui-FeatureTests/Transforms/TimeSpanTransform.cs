namespace BlueDotBrigade.Weevil.Gui.Transforms
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.TestTools.Text;
	using Newtonsoft.Json.Converters;

	[Binding]
	internal class TimeSpanTransform
	{
		[StepArgumentTransformation($"{X.TimePeriod}")]
		internal TimeSpan Transform(string text)
		{
			var pattern = RegexHelper.RevealGroups(X.TimePeriod);
			var results = Regex.Match(text, pattern);

			var value = int.Parse(results.Groups[1].Value);
			var unit = results.Groups[2].Value;

			switch (unit)
			{
				case "ms":
					return TimeSpan.FromMilliseconds(value);

				case "sec":
					return TimeSpan.FromSeconds(value);

				case "min":
					return TimeSpan.FromMinutes(value);

				default:
					throw new ArgumentOutOfRangeException(
							nameof(value),
							$"The unit of time is not supported. Unit={unit}");
			}
		}
	}
}