//namespace BlueDotBrigade.Weevil.StepDefinitions
//{
//	using BlueDotBrigade.Weevil.Data;

//	public class RecordContentValidator : AbstractValidator<IRecord>
//	{
//		public RecordContentValidator()
//		{
//			When(p => p.Name.Equals("sAMAccountName"), () =>
//			{
//				RuleFor(p => p.input)
//					.NotEmpty()
//					.MyOtherValidationRule();
//			});

//			When(p => p.Name.Equals("anotherName"), () =>
//			{
//				RuleFor(p => p.input)
//					.NotEmpty()
//					.HereItIsAnotherValidationRule();
//			});
//		}
//	}
//}
