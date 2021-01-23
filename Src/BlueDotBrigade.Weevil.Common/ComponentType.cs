namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	[Flags]
	public enum ComponentType
	{
		Core = 1,
		Extension = 2,
		All = int.MaxValue,
	}
}
