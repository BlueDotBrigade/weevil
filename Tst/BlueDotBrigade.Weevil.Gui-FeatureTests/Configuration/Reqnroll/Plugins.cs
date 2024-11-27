using System;
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

[assembly: RuntimePlugin(typeof(Plugins))]

namespace BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll;

//[StaTestClass]
public class Plugins : IRuntimePlugin
{
	public void Initialize(
		RuntimePluginEvents runtimePluginEvents,
		RuntimePluginParameters runtimePluginParameters,
		UnitTestProviderConfiguration unitTestProviderConfiguration)
	{
		// FAILS due to 
		// System.InvalidOperationException: 'Failed to set the specified COM apartment state. Current apartment state 'MTA'.'
		// Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

		runtimePluginEvents.CustomizeGlobalDependencies += (_, args) =>
		{
			// register our class as ICucumberExpressionDetector
			args.ObjectContainer.RegisterTypeAs<ForceRegexDetector, ICucumberExpressionDetector>();
		};
	}
}