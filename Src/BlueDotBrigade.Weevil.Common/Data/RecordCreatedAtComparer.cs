﻿namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	internal sealed class RecordCreatedAtComparer : Comparer<IRecord>
	{
		private const int GreaterX = 1;
		private const int EqualTimestamps = 0;
		private const int GreaterY = -1;

		public override int Compare(IRecord x, IRecord y)
		{
			//
			if (x.HasCreationTime && y.HasCreationTime)
			{
				return x.CreatedAt.CompareTo(y.CreatedAt);
			}
			else
			{
				if (!x.HasCreationTime && !y.HasCreationTime)
				{
					return EqualTimestamps;
				}
				else if (x.HasCreationTime)
				{
					return GreaterX;
				}
				else
				{
					// second.HasCreationTime
					return GreaterY;
				}
			}
		}
	}
}
