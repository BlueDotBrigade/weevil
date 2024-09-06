namespace BlueDotBrigade.Weevil.Transforms
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Filter;

	[Binding]
	internal class FilterTypeTransform
	{
		[StepArgumentTransformation($"{R.TextExpression}")]
		internal FilterType PolarDegreesToAngle(string filterType)
		{
			return Enum.Parse<FilterType>(filterType);
		}
	}
}