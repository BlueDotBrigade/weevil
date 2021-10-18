namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using BlueDotBrigade.Weevil.Data;

	internal sealed class CreatedAtComparer : MagnitudeComparer
	{
		private const int GreaterX = 1;
		private const int EqualTimestamps = 0;
		private const int GreaterY = -1;

		public override int Compare(IRecord x, IRecord y)
		{
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

		public override double CompareMagnitude(IRecord x, IRecord y)
		{
			if (x.HasCreationTime && y.HasCreationTime)
			{
				var delta = x.CreatedAt - y.CreatedAt;
				return delta.TotalMilliseconds;
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
