namespace BlueDotBrigade.Weevil.Gui.IO
{
	using FluentValidation;

	public class UserInputValidator : AbstractValidator<UserPromptDialog>
	{
		private const string AnyString = @"^.*$";

		public UserInputValidator(string regExPattern, string error)
		{
			if (string.IsNullOrWhiteSpace(regExPattern) || regExPattern == ".*")
			{
				RuleFor(dialog => dialog.UserInput)
					.NotEmpty().WithMessage("Input cannot be empty.");
			}
			else
			{
				var rulePattern = string.IsNullOrWhiteSpace(regExPattern)
					? AnyString
					: regExPattern;

				var ruleError = string.IsNullOrWhiteSpace(error)
					? "Invalid input format."
					: error;

				RuleFor(dialog => dialog.UserInput)
					.NotEmpty().WithMessage("Input cannot be empty.")
					.Matches(rulePattern).WithMessage(ruleError);
			}
		}
	}
}