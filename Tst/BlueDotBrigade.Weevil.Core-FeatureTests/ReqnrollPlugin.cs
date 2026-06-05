using BlueDotBrigade.Weevil.Configuration.Reqnroll;
using Reqnroll.Bindings.CucumberExpressions;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

// Reqnroll discovers runtime plugins via assembly attributes; keep registration next to the plugin type
// instead of the old .csproj AssemblyAttribute entry that referenced a removed type (ReqnrollExtension).
[assembly: RuntimePlugin(typeof(BlueDotBrigade.Weevil.ReqnrollPlugin))]

namespace BlueDotBrigade.Weevil
{
	internal class ReqnrollPlugin : IRuntimePlugin
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
}