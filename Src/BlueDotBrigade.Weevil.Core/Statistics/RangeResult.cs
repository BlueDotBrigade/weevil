namespace BlueDotBrigade.Weevil.Statistics
{
    internal record RangeResult(DateTime? StartAt, DateTime? EndAt)
    {
        public override string ToString()
        {
			TimeSpan period = TimeSpan.Zero;

            if (this.StartAt.HasValue && this.EndAt.HasValue)
            {
				period = this.EndAt.Value - this.StartAt.Value;
            }

			return period.ToHumanReadable();
		}
    }
}
