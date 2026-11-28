namespace BlueDotBrigade.Weevil.Transforms
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;

	[Binding()]
	internal class SeverityOptionTransform
	{
		[StepArgumentTransformation($"{X.ShowSeverityOption}")]
		internal SeverityType Transform(string value)
		{
			switch (value)
			{
				case "Show Debug":
					return SeverityType.Debug;

				case "Show Trace":
					return SeverityType.Verbose;

				default:
					throw new ArgumentOutOfRangeException(nameof(value), $"Value was expected to be either: {X.ShowSeverityOption}");
			}
		}
	}
}