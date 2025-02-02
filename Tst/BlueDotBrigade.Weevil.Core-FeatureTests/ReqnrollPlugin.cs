using BlueDotBrigade.Weevil.Configuration.Reqnroll;
using Reqnroll.Bindings.CucumberExpressions;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

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