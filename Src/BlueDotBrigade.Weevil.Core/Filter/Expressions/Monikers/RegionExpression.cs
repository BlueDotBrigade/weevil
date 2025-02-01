namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class RegionExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Region");

		private readonly IRegionManager _regionManager;

		public RegionExpression(string serializedExpression, IRegionManager regionManager)
		{
			_regionManager = regionManager;
		}

		public bool IsMatch(IRecord record)
		{
			return 
				_regionManager.TryStartsWith(record.LineNumber, out var region) ||
				_regionManager.TryEndsWith(record.LineNumber, out region);
		}
	}
}
