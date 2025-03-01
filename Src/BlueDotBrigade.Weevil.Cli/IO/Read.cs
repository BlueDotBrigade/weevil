namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

    internal class Read
    {
		/// <summary>
		/// Returns the user's input or the default value if the user does not provide any input.
		/// </summary>
		/// <param name="prompt">The message displayed to the user, prompting for input.</param>
		public static void Line(string prompt)
        {
            Console.Write(prompt + " ");
        }

		/// <summary>
		/// Returns the user's input or the default value if the user does not provide any input.
		/// </summary>
		/// <param name="prompt">The message displayed to the user, prompting for input.</param>
		/// <param name="defaultValue">The value to be used if the user does not provide any input.</param>
		/// <param name="value">Contains the user's input, or the default value.</param>
		/// <returns>Returns <see langword="true"/> if the user provides input, <see langword="false"/> otherwise.</returns>
		public static bool LineOrDefault(string prompt, string defaultValue, out string value)
        {
            Console.Write(prompt + " ");

            var userInput = Console.ReadLine();
            var useDefault = string.IsNullOrWhiteSpace(userInput);

            value = useDefault ? defaultValue : userInput;

            return !useDefault;
        }
    }
}
