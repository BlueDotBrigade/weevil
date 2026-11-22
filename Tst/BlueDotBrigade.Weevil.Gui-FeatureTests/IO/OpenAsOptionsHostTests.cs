namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using BlueDotBrigade.Weevil.Windows.IO;

	/// <summary>
	/// A plugin shows its own options in the Open As dialog in place of Weevil's, while one of its logs is selected (#1216).
	/// </summary>
	[TestClass]
	public class OpenAsOptionsHostTests
	{
		private static readonly BrowserItem PluginLog = Log("Vendor.log");

		private static readonly BrowserItem PlainLog = Log("Plain.log");

		[TestMethod]
		public async Task GivenALogWhosePluginHasItsOwnOptions_WhenSelected_ThenThePluginsOptionsAreShown()
		{
			var weevil = new FakeOptions();
			var plugin = new FakePlugin();
			var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindPlugin(plugin));

			await host.SelectAsync(PluginLog);

			host.Options.Should().BeSameAs(plugin.Options);
			host.View.Should().Be("The plugin's view");
			plugin.Options.Selected.Should().Be(PluginLog);
			weevil.Selected.Should().BeNull("Weevil's options are not shown");
		}

		[TestMethod]
		public async Task GivenThePluginsOptionsShown_WhenALogOfNoSuchPluginIsSelected_ThenWeevilsOptionsAreShownAgain()
		{
			var weevil = new FakeOptions();
			var plugin = new FakePlugin();
			var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindPlugin(plugin));
			await host.SelectAsync(PluginLog);
			FakeOptions pluginOptions = plugin.Options;

			await host.SelectAsync(PlainLog);
			host.Options.Should().BeSameAs(weevil);
			host.View.Should().Be("Weevil's view");

			await host.SelectAsync(PluginLog);
			host.Options.Should().BeSameAs(pluginOptions, "the plugin's options are kept while the dialog is open, so they can remember a choice");
			plugin.Created.Should().Be(1);
		}

		[TestMethod]
		public async Task GivenThePluginsOptionsShown_WhenTheyBecomeComplete_ThenOpenIsEnabled()
		{
			var plugin = new FakePlugin();
			var host = new OpenAsOptionsHost(new FakeOptions { CanOpen = true }, "Weevil's view", FindPlugin(plugin));
			await host.SelectAsync(PluginLog);
			host.CanOpen.Should().BeFalse("the plugin's options are not complete yet");
			var changes = 0;
			host.Changed += (_, _) => changes++;

			plugin.Options.CanOpen = true;

			host.CanOpen.Should().BeTrue();
			changes.Should().Be(1, "the dialog is told, so it enables Open");
		}

		[TestMethod]
		public async Task GivenAPluginThatFails_WhenItsLogIsSelected_ThenWeevilsOptionsAreShown()
		{
			// Browsing must never stop Weevil.
			var weevil = new FakeOptions();
			var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindPlugin(new FakePlugin { IsFailing = true }));

			await host.SelectAsync(PluginLog);
			host.Options.Should().BeSameAs(weevil);

			host = new OpenAsOptionsHost(weevil, "Weevil's view", _ => throw new InvalidOperationException("The plugin could not read the log."));
			await host.SelectAsync(PluginLog);
			host.Options.Should().BeSameAs(weevil);
			weevil.Selected.Should().Be(PluginLog);
		}

		private static Func<BrowserItem, IOpenAsOptionsPlugin> FindPlugin(FakePlugin plugin)
		{
			return file => file == PluginLog ? plugin : null;
		}

		private static BrowserItem Log(string name)
		{
			return new BrowserItem(Location.ForFileSystem(Path.Combine(Path.GetTempPath(), name)), name, ItemKind.File, size: 1_000);
		}

		private sealed class FakePlugin : IOpenAsOptionsPlugin
		{
			public FakeOptions Options { get; private set; }

			public int Created { get; private set; }

			public bool IsFailing { get; init; }

			public IOpenAsOptions CreateOpenAsOptions()
			{
				if (this.IsFailing)
				{
					throw new InvalidOperationException("The plugin could not create its options.");
				}

				this.Created++;
				this.Options = new FakeOptions();
				return this.Options;
			}

			public object CreateOpenAsView(IOpenAsOptions options)
			{
				return "The plugin's view";
			}
		}

		private sealed class FakeOptions : IOpenAsOptions, INotifyPropertyChanged
		{
			private bool _canOpen;

			public event PropertyChangedEventHandler? PropertyChanged;

			public BrowserItem? Selected { get; private set; }

			public bool CanOpen
			{
				get => _canOpen;
				set
				{
					_canOpen = value;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CanOpen)));
				}
			}

			public StorageMode StorageMode => StorageMode.Ram;

			public LogPart Part => LogPart.Whole;

			public ContextDictionary Context { get; } = new ContextDictionary();

			public Task SelectAsync(BrowserItem file)
			{
				this.Selected = file;
				return Task.CompletedTask;
			}
		}
	}
}
