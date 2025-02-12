namespace BlueDotBrigade.Weevil.Gui.IO
{
       public class UserInputValidator : AbstractValidator<UserPromptDialog>
    {
        public UserInputValidator(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == ".*")
            {
                RuleFor(dialog => dialog.UserInput)
                    .NotEmpty().WithMessage("Input cannot be empty.");
            }
            else
            {
                RuleFor(dialog => dialog.UserInput)
                    .NotEmpty().WithMessage("Input cannot be empty.")
                    .Matches(pattern).WithMessage("Invalid input format.");
            }
        }
    }
}