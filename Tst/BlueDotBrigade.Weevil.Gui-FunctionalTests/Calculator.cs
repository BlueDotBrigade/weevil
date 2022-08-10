namespace BlueDotBrigade.Weevil.Gui
{
	using System;

	public class Calculator
	{
		public int FirstNumber { get; set; }
		public int SecondNumber { get; set; }

		public int Add()
		{
			return this.FirstNumber + this.SecondNumber;
		}

		public int Subtract()
		{
			return this.FirstNumber - this.SecondNumber;
		}
	}
}
