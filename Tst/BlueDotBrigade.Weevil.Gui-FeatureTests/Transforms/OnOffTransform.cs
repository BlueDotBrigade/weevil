using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueDotBrigade.Weevil.Filter;

namespace BlueDotBrigade.Weevil.Gui.Transforms
{
	[Binding()]
	internal class OnOffTransform
	{
		[StepArgumentTransformation($"{X.OnOff}")]
		internal bool Transform(string value)
		{
			switch (value)
			{
				case "on":
					return true;

				case "off":
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(value), $"Value was expected to be either: {X.OnOff}");
			}
		}
	}
}