namespace BlueDotBrigade.Weevil.Windows.IO
{
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
	/// <ComboBox ItemsSource="{Binding ListOfOptions}" IsEditable="True">
	///		<ComboBox.Text>
	///			<Binding Path = "SelectedOption" UpdateSourceTrigger="PropertyChanged">
	///				<Binding.ValidationRules>
	///					<SelectedItemValidationRule
	///						ValidatesOnTargetUpdated = "True"
	///						ErrorMessage="Custom error message goes here."/>
	///				</Binding.ValidationRules>
	///			</Binding>
	///		</ComboBox.Text>
	///	</ComboBox>
	/// ]]>
	/// </code>
	/// </remarks>
	public class SelectedItemValidationRule : ValidationRule
	{
		private static readonly object NoItemSelected = null;
		private string _errorMessage;

		private const string DefaultMessage = @"A ComboBox item should be selected.";

		private readonly bool _isInWpfDesigner;

		/// <summary>
		/// Validation fails when a ComboBox item has not been selected.
		/// </summary>
		public SelectedItemValidationRule()
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

		/// <summary>
		/// Returns a validation error when a ComboBox item has not been selected.
		/// </summary>
		/// <param name="selectedValue">Bound to the SelectedValue property of a WPF ComboBox.</param>
		/// <param name="cultureInfo">The culture to use with this rule.</param>
		/// <returns></returns>
		/// <seealso cref="ComboBox"/>
		public override ValidationResult Validate(object selectedValue, CultureInfo cultureInfo)
		{
			var isItemSelected = selectedValue != NoItemSelected;

			return isItemSelected
				? new ValidationResult(true, null)
				: new ValidationResult(false, this.ErrorMessage);
		}
	}
}
