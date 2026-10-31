namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	internal sealed class CreatedAtComparer : MagnitudeComparer<DateTime>
	{
		private const int GreaterX = 1;
		private const int EqualTimestamps = 0;
		private const int GreaterY = -1;

		public override DateTime Read(RecordList records, int position)
		{
			return records.GetCreatedAt(position);
		}

		public override int Compare(DateTime x, DateTime y)
		{
			if (HasCreationTime(x) && HasCreationTime(y))
			{
				return x.CompareTo(y);
			}
			else
			{
				if (!HasCreationTime(x) && !HasCreationTime(y))
				{
					return EqualTimestamps;
				}
				else if (HasCreationTime(x))
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

		public override double CompareMagnitude(DateTime x, DateTime y)
		{
			if (HasCreationTime(x) && HasCreationTime(y))
			{
				var delta = x - y;
				return delta.TotalMilliseconds;
			}
			else
			{
				if (!HasCreationTime(x) && !HasCreationTime(y))
				{
					return EqualTimestamps;
				}
				else if (HasCreationTime(x))
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

		private static bool HasCreationTime(DateTime value)
		{
			return value != Record.CreationTimeUnknown;
		}
	}
}
