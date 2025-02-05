using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlueDotBrigade;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Gui;
using BlueDotBrigade.Weevil.Gui;
using BlueDotBrigade.Weevil.Gui.Configuration;
using BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll;
using Reqnroll.Bindings.CucumberExpressions;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

[assembly: RuntimePlugin(typeof(ReqnrollPlugin))]

namespace BlueDotBrigade.Weevil.Gui;

public class ReqnrollPlugin : IRuntimePlugin
{
	public void Initialize(
		RuntimePluginEvents runtimePluginEvents,
		RuntimePluginParameters runtimePluginParameters,
		UnitTestProviderConfiguration unitTestProviderConfiguration)
	{
		runtimePluginEvents.CustomizeGlobalDependencies += (_, args) =>
		{
			// register our class as ICucumberExpressionDetector
			args.ObjectContainer.RegisterTypeAs<ForceRegexExpressions, ICucumberExpressionDetector>();
		};
	}
}