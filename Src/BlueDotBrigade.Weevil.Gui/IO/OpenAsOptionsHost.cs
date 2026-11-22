namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using BlueDotBrigade.Weevil.Windows.IO;

	/// <summary>
	/// Chooses whose options the Open As dialog shows for the log selected: those of the log's plugin, when it supplies its own, or else Weevil's (#1216).
	/// </summary>
	/// <remarks>
	/// A plugin's options are created the first time one of its logs is selected, kept while the dialog is open, as Weevil's are, and disposed with this host.
	/// Open stays disabled from a selection until the options shown have started to show that log, so the log never opens with what was chosen for the previous one.
	/// </remarks>
	internal sealed class OpenAsOptionsHost : IDisposable
	{
		private readonly IOpenAsOptions _weevilOptions;
		private readonly object _weevilView;
		private readonly Func<BrowserItem, IOpenAsOptionsProvider> _findProvider;
		private readonly Dictionary<IOpenAsOptionsProvider, (IOpenAsOptions Options, object View)> _pluginOptions = new Dictionary<IOpenAsOptionsProvider, (IOpenAsOptions Options, object View)>();

		/// <summary>
		/// Counts the selections, so the provider found for a log no longer selected is ignored.
		/// </summary>
		private int _selection;

		/// <summary>
		/// Whose options to show for the latest selection is not settled yet.
		/// </summary>
		private bool _isChoosing;

		/// <param name="weevilOptions">Weevil's options, shown unless the log's plugin supplies its own.</param>
		/// <param name="weevilView">What the dialog shows for <paramref name="weevilOptions"/>.</param>
		/// <param name="findProvider">Finds the options provider of a log's plugin, off the UI thread, or returns <see langword="null"/> when its plugin has none.</param>
		public OpenAsOptionsHost(IOpenAsOptions weevilOptions, object weevilView, Func<BrowserItem, IOpenAsOptionsProvider> findProvider)
		{
			_weevilOptions = weevilOptions ?? throw new ArgumentNullException(nameof(weevilOptions));
			_weevilView = weevilView;
			_findProvider = findProvider ?? throw new ArgumentNullException(nameof(findProvider));

			this.Options = weevilOptions;
			this.View = weevilView;
			Watch(weevilOptions);
		}

		/// <summary>
		/// Raised on the UI thread when other options are shown, or when whether the log can be opened may have changed.
		/// </summary>
		public event EventHandler Changed;

		/// <summary>
		/// The options shown, which say how to open the log.
		/// </summary>
		public IOpenAsOptions Options { get; private set; }

		/// <summary>
		/// What the dialog shows for <see cref="Options"/>.
		/// </summary>
		public object View { get; private set; }

		/// <summary>
		/// Whether the log selected can be opened: never while whose options to show is being worked out.
		/// </summary>
		public bool CanOpen => !_isChoosing && this.Options.CanOpen;

		/// <summary>
		/// Shows the options of the log's plugin, or Weevil's, for the item selected: a log, or <see langword="null"/> when a folder, a zip or nothing is selected.
		/// </summary>
		/// <remarks>
		/// The provider is found off the UI thread, since a plugin may read the log to tell whether it is one of its logs.
		/// </remarks>
		public async Task SelectAsync(BrowserItem file)
		{
			var selection = ++_selection;

			_isChoosing = true;
			OnChanged();

			IOpenAsOptionsProvider provider = file is { IsFile: true }
				? await Task.Run(() => FindProvider(file))
				: null;

			if (selection != _selection)
			{
				return;
			}

			Show(provider);

			// The options start to show the log, which resets what they say, before Open is enabled again.
			Task selecting = SelectAsync(this.Options, file, selection);
			_isChoosing = false;
			OnChanged();

			await selecting;

			// For options that don't say when they change.
			OnChanged();
		}

		/// <summary>
		/// Disposes the plugins' options that can be, once the dialog is closed and how to open the log has been read.
		/// </summary>
		public void Dispose()
		{
			Unwatch(this.Options);

			foreach ((IOpenAsOptions options, _) in _pluginOptions.Values)
			{
				if (!ReferenceEquals(options, _weevilOptions) && options is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch (Exception e)
					{
						Log.Default.Write(LogSeverityType.Warning, e, $"A plugin's Open As options could not be disposed. Options={options.GetType().FullName}");
					}
				}
			}

			_pluginOptions.Clear();
		}

		/// <summary>
		/// Shows the log in <paramref name="options"/>. When a plugin's options fail, Weevil's are shown in their place: browsing must never stop Weevil.
		/// </summary>
		private async Task SelectAsync(IOpenAsOptions options, BrowserItem file, int selection)
		{
			try
			{
				await options.SelectAsync(file);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As options could not show a file. Options={options.GetType().FullName}, File={file?.Name}");

				if (!ReferenceEquals(options, _weevilOptions) && selection == _selection)
				{
					// Gated as a selection is, so Weevil's options aren't offered with what they said for the previous log.
					_isChoosing = true;
					Show(provider: null);
					Task selecting = SelectAsync(_weevilOptions, file, selection);
					_isChoosing = false;
					OnChanged();
					await selecting;
				}
			}
		}

		/// <summary>
		/// The options provider of the log's plugin, or none when it can't be found: browsing must never stop Weevil.
		/// </summary>
		private IOpenAsOptionsProvider FindProvider(BrowserItem file)
		{
			try
			{
				return _findProvider(file);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As could not find the plugin of a file, so it shows Weevil's options. File={file.Name}");
				return null;
			}
		}

		private void Show(IOpenAsOptionsProvider provider)
		{
			(IOpenAsOptions options, object view) = provider is null ? (_weevilOptions, _weevilView) : GetOptions(provider);

			if (ReferenceEquals(options, this.Options))
			{
				return;
			}

			Unwatch(this.Options);
			this.Options = options;
			this.View = view;
			Watch(options);

			OnChanged();
		}

		/// <summary>
		/// The provider's options, created the first time, or Weevil's when the provider fails to create them.
		/// </summary>
		private (IOpenAsOptions Options, object View) GetOptions(IOpenAsOptionsProvider provider)
		{
			if (!_pluginOptions.TryGetValue(provider, out (IOpenAsOptions Options, object View) created))
			{
				try
				{
					IOpenAsOptions options = provider.CreateOpenAsOptions() ?? throw new InvalidOperationException("The plugin created no options.");
					created = (options, provider.CreateOpenAsView(options));
				}
				catch (Exception e)
				{
					Log.Default.Write(LogSeverityType.Warning, e, $"A plugin could not create its Open As options, so Weevil's are shown. Plugin={provider.PluginName}");
					created = (_weevilOptions, _weevilView);
				}

				_pluginOptions[provider] = created;
			}

			return created;
		}

		private void Watch(IOpenAsOptions options)
		{
			if (options is INotifyPropertyChanged notifying)
			{
				notifying.PropertyChanged += OnOptionsChanged;
			}
		}

		private void Unwatch(IOpenAsOptions options)
		{
			if (options is INotifyPropertyChanged notifying)
			{
				notifying.PropertyChanged -= OnOptionsChanged;
			}
		}

		private void OnOptionsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(IOpenAsOptions.CanOpen))
			{
				OnChanged();
			}
		}

		private void OnChanged()
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
