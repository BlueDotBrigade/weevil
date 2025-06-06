﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlueDotBrigade;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll;
using Reqnroll.Bindings.CucumberExpressions;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

namespace BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll;

/// <seealso href="https://github.com/reqnroll/Reqnroll/blob/b8988c7311eb677852f83b4a1d0679a7f0e39371/docs/guides/how-to-configure-cucumber-expression-behavior.md">Reqnroll: Force using RegEx</seealso>
/// <seealso href="https://github.com/reqnroll/Reqnroll/commit/812ffc8c06c647dfbd869507a34e60ac41927eeb">Reqnroll: Added option to override regex group matching behavior</seealso>
public class ForceRegexExpressions : ICucumberExpressionDetector
{
	public bool IsCucumberExpression(string expression)
	{
		// Similar to Reqnroll's predecessor SpecFlow, assume that all expressions are regex by default.
		return false;
	}
}