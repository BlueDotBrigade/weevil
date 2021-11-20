namespace BlueDotBrigade.Weevil.Windows.IO
{
	using System.Globalization;
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
	///					<SelectedItemValidationRule
	///						ValidatesOnTargetUpdated = "True"
	///						ErrorMessage="Custom error message goes here."/>
	///				</Binding.ValidationRules>
	///			</Binding>
	///		</ComboBox.SelectedValue>
	///	</ComboBox>
	/// ]]>
	/// </code>
	/// </remarks>
	public class SelectedItemValidationRule : ValidationRule
	{
		private static readonly object NoItemSelected = null;

		private const string DefaultMessage = @"A ComboBox item should be selected.";

		/// <summary>
		/// Validation fails when a ComboBox item has not been selected.
		/// </summary>
		public SelectedItemValidationRule()
		{
			this.ErrorMessage = DefaultMessage;

			Validate(NoItemSelected, CultureInfo.InvariantCulture);
		}

		public string ErrorMessage { get; set; }

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
