namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System;
	using System.Globalization;
	using System.Windows.Controls;

	public class VersionValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var result = new ValidationResult(false, "Value is expected to be in the format: x.y.z");

			if (value != null)
			{
				if (Version.TryParse(value.ToString(), out Version versionValue))
				{
					result = new ValidationResult(true, null);
				}
			}

			return result;
		}
	}
}