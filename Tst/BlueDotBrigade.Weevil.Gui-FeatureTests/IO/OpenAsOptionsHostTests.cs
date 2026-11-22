namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Threading;
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
			var provider = new FakeProvider();
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindProvider(provider));

			await host.SelectAsync(PluginLog);

			host.Options.Should().BeSameAs(provider.Options);
			host.View.Should().Be("The plugin's view");
			provider.Options!.Selected.Should().Be(PluginLog);
			weevil.Selected.Should().BeNull("Weevil's options are not shown");
		}

		[TestMethod]
		public async Task GivenThePluginsOptionsShown_WhenALogWithoutThemIsSelected_ThenWeevilsOptionsAreShownAgain()
		{
			var weevil = new FakeOptions();
			var provider = new FakeProvider();
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindProvider(provider));
			await host.SelectAsync(PluginLog);
			FakeOptions? pluginOptions = provider.Options;

			await host.SelectAsync(PlainLog);
			host.Options.Should().BeSameAs(weevil);
			host.View.Should().Be("Weevil's view");

			await host.SelectAsync(PluginLog);
			host.Options.Should().BeSameAs(pluginOptions, "the plugin's options are kept while the dialog is open, so they can remember a choice");
			provider.Created.Should().Be(1);
		}

		[TestMethod]
		public async Task GivenThePluginsOptionsShown_WhenTheyBecomeComplete_ThenOpenIsEnabled()
		{
			var provider = new FakeProvider();
			using var host = new OpenAsOptionsHost(new FakeOptions { CanOpen = true }, "Weevil's view", FindProvider(provider));
			await host.SelectAsync(PluginLog);
			host.CanOpen.Should().BeFalse("the plugin's options are not complete yet");
			var changes = 0;
			host.Changed += (_, _) => changes++;

			provider.Options!.CanOpen = true;

			host.CanOpen.Should().BeTrue();
			changes.Should().Be(1, "the dialog is told, so it enables Open");
		}

		[TestMethod]
		public async Task GivenTheLogBeforeCouldBeOpened_WhenAnotherIsSelected_ThenOpenWaitsUntilItsOptionsAreChosen()
		{
			// While the new log's plugin was being found, Open opened it with the part and mode chosen for the log before.
			using var finding = new ManualResetEventSlim();
			var weevil = new FakeOptions { CanOpen = true };
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", file =>
			{
				if (file == PluginLog)
				{
					finding.Wait(TimeSpan.FromSeconds(10));
				}

				return null;
			});
			await host.SelectAsync(PlainLog);
			host.CanOpen.Should().BeTrue();

			Task selecting = host.SelectAsync(PluginLog);
			host.CanOpen.Should().BeFalse("whose options to show for the new log is not settled yet");

			finding.Set();
			await selecting;
			host.CanOpen.Should().BeTrue();
			weevil.Selected.Should().Be(PluginLog);
		}

		[TestMethod]
		public async Task GivenALogStillBeingChecked_WhenAnotherIsSelected_ThenTheFirstCheckIsIgnored()
		{
			// A plugin on a slow network drive must not show its options for the log selected after it.
			using var finding = new ManualResetEventSlim();
			var weevil = new FakeOptions();
			var provider = new FakeProvider();
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", file =>
			{
				if (file == PluginLog)
				{
					finding.Wait(TimeSpan.FromSeconds(10));
					return provider;
				}

				return null;
			});

			Task slowSelection = host.SelectAsync(PluginLog);
			await host.SelectAsync(PlainLog);
			finding.Set();
			await slowSelection;

			host.Options.Should().BeSameAs(weevil);
			weevil.Selected.Should().Be(PlainLog);
			provider.Created.Should().Be(0);
		}

		[TestMethod]
		public async Task GivenAPluginThatFailsToCreateItsOptions_WhenItsLogIsSelected_ThenWeevilsOptionsAreShown()
		{
			// Browsing must never stop Weevil.
			var weevil = new FakeOptions();
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindProvider(new FakeProvider { IsCreateFailing = true }));

			await host.SelectAsync(PluginLog);

			host.Options.Should().BeSameAs(weevil);
			weevil.Selected.Should().Be(PluginLog);
		}

		[TestMethod]
		public async Task GivenPluginOptionsThatFailToShowALog_WhenItIsSelected_ThenWeevilsOptionsAreShown()
		{
			// The dialog's selection handler can't catch it, so it would have closed Weevil.
			var weevil = new FakeOptions();
			using var host = new OpenAsOptionsHost(weevil, "Weevil's view", FindProvider(new FakeProvider { IsSelectFailing = true }));

			await host.SelectAsync(PluginLog);

			host.Options.Should().BeSameAs(weevil);
			weevil.Selected.Should().Be(PluginLog);
		}

		[TestMethod]
		public async Task GivenPluginOptionsShownInTheDialog_WhenItCloses_ThenTheyAreDisposed()
		{
			// Options that previewed the log would otherwise keep it from opening (#1254).
			var provider = new FakeProvider();
			var host = new OpenAsOptionsHost(new FakeOptions(), "Weevil's view", FindProvider(provider));
			await host.SelectAsync(PluginLog);

			host.Dispose();

			provider.Options!.IsDisposed.Should().BeTrue();
		}

		private static Func<BrowserItem, IOpenAsOptionsProvider?> FindProvider(FakeProvider provider)
		{
			return file => file == PluginLog ? provider : null;
		}

		private static BrowserItem Log(string name)
		{
			return new BrowserItem(Location.ForFileSystem(Path.Combine(Path.GetTempPath(), name)), name, ItemKind.File, size: 1_000);
		}

		private sealed class FakeProvider : IOpenAsOptionsProvider
		{
			public string PluginName => "Vendor";

			public FakeOptions? Options { get; private set; }

			public int Created { get; private set; }

			public bool IsCreateFailing { get; init; }

			public bool IsSelectFailing { get; init; }

			public IOpenAsOptions CreateOpenAsOptions()
			{
				if (this.IsCreateFailing)
				{
					throw new InvalidOperationException("The plugin could not create its options.");
				}

				this.Created++;
				this.Options = new FakeOptions { IsSelectFailing = this.IsSelectFailing };
				return this.Options;
			}

			public object CreateOpenAsView(IOpenAsOptions options)
			{
				return "The plugin's view";
			}
		}

		private sealed class FakeOptions : IOpenAsOptions, INotifyPropertyChanged, IDisposable
		{
			private bool _canOpen;

			public event PropertyChangedEventHandler? PropertyChanged;

			public BrowserItem? Selected { get; private set; }

			public bool IsSelectFailing { get; init; }

			public bool IsDisposed { get; private set; }

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

			public async Task SelectAsync(BrowserItem file)
			{
				await Task.Yield();

				if (this.IsSelectFailing)
				{
					throw new InvalidOperationException("The plugin's options could not read the log.");
				}

				this.Selected = file;
			}

			public void Dispose()
			{
				this.IsDisposed = true;
			}
		}
	}
}
