namespace BlueDotBrigade.Weevil.Gui_FunctionalTests.StepDefinitions
{
	using BlueDotBrigade.Weevil.Gui;
	using FluentAssertions;

	[Binding]
    public sealed class CalculatorStepDefinitions
    {
        // For additional details on SpecFlow step definitions see https://go.specflow.org/doc-stepdef

        private Calculator _calculator;
        private int _actualResult;
		

        [Given("the first number is (.*)")]
        public void GivenTheFirstNumberIs(int number)
        {
			//TODO: implement arrange (precondition) logic
			// For storing and retrieving scenario-specific data see https://go.specflow.org/doc-sharingdata
			// To use the multiline text or the table argument of the scenario,
			// additional string/Table parameters can be defined on the step definition
			// method. 

			_calculator = new Calculator { FirstNumber = number };

        }

        [Given("the second number is (.*)")]
        public void GivenTheSecondNumberIs(int number)
        {
            //TODO: implement arrange (precondition) logic

            _calculator.SecondNumber = number;
        }

        [When("the two numbers are added")]
        public void WhenTheTwoNumbersAreAdded()
        {
			//TODO: implement act (action) logic

			_actualResult = _calculator.Add();
        }

        [When(@"the two numbers are subtracted")]
        public void WhenTheTwoNumbersAreSubtracted()
        {
			_actualResult = _calculator.Subtract();
		}


		[Then("the result should be (.*)")]
        public void ThenTheResultShouldBe(int expectedResult)
        {
			//TODO: implement assert (verification) logic

			_actualResult.Should().Be(expectedResult);
        }
    }
}