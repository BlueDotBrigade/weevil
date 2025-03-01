namespace BlueDotBrigade.Weevil.Gui.IO
{
    using System;
	using System.ComponentModel;
	using System.Text.RegularExpressions;
	using System.Windows;
    using FluentValidation;
    using FluentValidation.Results;
	using static System.Net.Mime.MediaTypeNames;
	using Application = System.Windows.Application;

	/// <summary>
	/// Interaction logic for UserPromptDialog.xaml
	/// </summary>
	public partial class UserPromptDialog : Window, INotifyPropertyChanged
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

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string name)
		{
			var handler = this.PropertyChanged;

			if (handler != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}

		private string _validationMessage;

		public string ValidationMessage
		{
			get => _validationMessage;
			private set
			{
				_validationMessage = value;
				RaisePropertyChanged(nameof(this.ValidationMessage));
				RaisePropertyChanged(nameof(IsValidationMessageVisible));
			}
		}

		public bool IsValidationMessageVisible
		{
			get => !string.IsNullOrWhiteSpace(ValidationMessage);
		}

		public UserPromptDialog() : this (string.Empty, string.Empty)
		{
			// nothing to do
		}

		public UserPromptDialog(string validationPattern, string validationError)
		{
			this.Owner = Application.Current.MainWindow;
			this.Loaded += OnDialogLoaded;
			InitializeComponent();
			this.DataContext = this;

			_validator = new UserInputValidator(validationPattern, validationError);
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
