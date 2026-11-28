namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	internal sealed class CreatedAtComparer : MagnitudeComparer<DateTime>
	{
		private const int FirstIsGreater = 1;
		private const int EqualTimestamps = 0;
		private const int SecondIsGreater = -1;

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
					return FirstIsGreater;
				}
				else
				{
					// second.HasCreationTime
					return SecondIsGreater;
				}
			}
		}

		public override double CompareMagnitude(DateTime left, DateTime right)
		{
			if (HasCreationTime(left) && HasCreationTime(right))
			{
				TimeSpan delta = left - right;
				return delta.TotalMilliseconds;
			}
			else
			{
				if (!HasCreationTime(left) && !HasCreationTime(right))
				{
					return EqualTimestamps;
				}
				else if (HasCreationTime(left))
				{
					return FirstIsGreater;
				}
				else
				{
					// second.HasCreationTime
					return SecondIsGreater;
				}
			}
		}

		private static bool HasCreationTime(DateTime value)
		{
			return value != Record.CreationTimeUnknown;
		}
	}
}
