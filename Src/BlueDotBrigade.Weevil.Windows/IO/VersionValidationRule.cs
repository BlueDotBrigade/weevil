namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System;
	using System.Globalization;
	using System.Windows.Controls;

	public class VersionValidationRule : ValidationRule
	{
		private const string DefaultMessage = @"Value is expected to be in the format: x.y.z";

		public VersionValidationRule()
		{
			this.ErrorMessage = DefaultMessage;
		}

		public string ErrorMessage { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var result = new ValidationResult(false, this.ErrorMessage);

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