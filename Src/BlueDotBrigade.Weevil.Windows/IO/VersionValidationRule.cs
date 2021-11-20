namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Validation fails when a ComboBox item has not been selected.
	/// </summary>
	/// <remarks>
	///  The following sample code shows how to use the validator in XAML:
	/// <code>
	/// <![CDATA[
	/// <ComboBox ItemsSource="{Binding ListOfOptions}">
	///		<ComboBox.SelectedValue>
	///			<Binding Path = "SelectedOption" UpdateSourceTrigger="PropertyChanged">
	///				<Binding.ValidationRules>
	///					<VersionValidationRule
	///						ValidatesOnTargetUpdated = "True"
	///						ErrorMessage="Custom error message goes here."/>
	///				</Binding.ValidationRules>
	///			</Binding>
	///		</ComboBox.SelectedValue>
	///	</ComboBox>
	/// ]]>
	/// </code>
	/// </remarks>
	public class VersionValidationRule : ValidationRule
	{
		private string _errorMessage;
		private const string DefaultMessage = @"Value is expected to be in the format: x.y.z";

		private readonly bool _isInWpfDesigner;

		public VersionValidationRule()
		{
			_errorMessage = DefaultMessage;
			_isInWpfDesigner = System.Reflection.Assembly.GetExecutingAssembly().Location.Contains("VisualStudio");
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set => _errorMessage = value;
		}

		/// <summary>
		/// When true, the validation rule is also called during source-to-target data
		/// transfer.  This allows invalid data in the source to be highlighted
		/// as soon as it appears in the UI, without waiting for the user to edit it.
		/// </summary>
		/// <remarks>
		/// When <see langword="True"/>, this property will force the validation rule
		/// to be run when the control is created.
		/// </remarks>
		public new bool ValidatesOnTargetUpdated
		{
			get
			{
				return base.ValidatesOnTargetUpdated;
			}
			set
			{
				// HACK: The Visual Studio WPF designer will crash with NullReferenceException when ValidatesOnTargetUpdated=True.
				// https://stackoverflow.com/questions/45708488/validationrule-validatesontargetupdated-nullreferenceexception-at-design-time
				// https://newbedev.com/is-there-a-designmode-property-in-wpf
				base.ValidatesOnTargetUpdated = !_isInWpfDesigner && value;
			}
		}

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