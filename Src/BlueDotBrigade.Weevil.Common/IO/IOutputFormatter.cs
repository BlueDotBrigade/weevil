namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public interface IOutputFormatter
	{
		string AsText(string message);
		string AsHeading(string message);
		string AsBullet(string message);
		string AsNumbered(string message);
		string AsError(string message);
		string AsTable(string[] headers, string[][] rows);
		void ResetNumbering();
	}
}
