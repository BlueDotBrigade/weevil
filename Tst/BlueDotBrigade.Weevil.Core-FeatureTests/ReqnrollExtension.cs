namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Reqnroll.Plugins;
	using Reqnroll.UnitTestProvider;

	internal class ReqnrollExtension : IRuntimePlugin
	{
		public void Initialize(
			RuntimePluginEvents runtimePluginEvents, 
			RuntimePluginParameters runtimePluginParameters,
			UnitTestProviderConfiguration unitTestProviderConfiguration)
		{
			// nothing to do
		}
	}
}