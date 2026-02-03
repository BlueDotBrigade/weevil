namespace BlueDotBrigade.Weevil.IO
{
	using System.Diagnostics;
	using System;

	public interface IOutputFormatter
	{
		string AsText(string message);
		string AsHeading(string message);
		string AsBullet(string message);
		string AsNumbered(string message);
		string AsError(string message);
		void ResetNumbering();
	}
}
