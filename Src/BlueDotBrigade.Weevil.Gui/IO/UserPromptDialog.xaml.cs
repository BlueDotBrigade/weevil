namespace BlueDotBrigade.Weevil.Gui.IO
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using FluentValidation;
    using FluentValidation.Results;

    /// <summary>
    /// Interaction logic for UserPromptDialog.xaml
    /// </summary>
    public partial class UserPromptDialog : Window
    {
        public static readonly DependencyProperty UserPromptProperty =
            DependencyProperty.Register(
                nameof(UserPrompt), typeof(string),
                typeof(UserPromptDialog));

        public static readonly DependencyProperty UserInputProperty =
            DependencyProperty.Register(
                nameof(UserInput), typeof(string),
                typeof(UserPromptDialog),
                new FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = true,
                });

        public static readonly DependencyProperty ValidationPatternProperty =
            DependencyProperty.Register(
                nameof(ValidationPattern), typeof(string),
                typeof(UserPromptDialog),
                new PropertyMetadata(".*", OnValidationPatternChanged));

        private UserInputValidator _validator;

        public string UserPrompt
        {
            get => (string)GetValue(UserPromptProperty);
            set => SetValue(UserPromptProperty, value);
        }

        public string UserInput
        {
            get => (string)GetValue(UserInputProperty);
            set
            {
                SetValue(UserInputProperty, value);
                ValidateUserInput();
            }
        }

        public string ValidationPattern
        {
            get => (string)GetValue(ValidationPatternProperty);
            set => SetValue(ValidationPatternProperty, value);
        }

        public string ValidationMessage { get; private set; }

        public UserPromptDialog(Window parentWindow)
        {
            this.Owner = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            this.Loaded += OnDialogLoaded;
            InitializeComponent();
            this.DataContext = this;
            _validator = new UserInputValidator(ValidationPattern);
        }

        private static void OnValidationPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserPromptDialog dialog)
            {
                dialog._validator = new UserInputValidator((string)e.NewValue);
            }
        }

        private void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            this.InputTextBox.SelectAll();
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            var validationResult = _validator?.Validate(this);
            if (validationResult != null && !validationResult.IsValid)
            {
                ValidationMessage = validationResult.Errors[0].ErrorMessage;
                return;
            }

            this.DialogResult = true;
        }

        private void ValidateUserInput()
        {
            if (_validator != null)
            {
                var validationResult = _validator.Validate(this);
                ValidationMessage = validationResult.IsValid ? string.Empty : validationResult.Errors[0].ErrorMessage;
            }
        }
    }
}
