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
	/// A plugin's options are created the first time one of its logs is selected, and kept while the dialog is open, as Weevil's are.
	/// </remarks>
	internal sealed class OpenAsOptionsHost
	{
		private readonly IOpenAsOptions _weevilOptions;
		private readonly object _weevilView;
		private readonly Func<BrowserItem, IOpenAsOptionsPlugin> _findPlugin;
		private readonly Dictionary<Type, (IOpenAsOptions Options, object View)> _pluginOptions = new Dictionary<Type, (IOpenAsOptions Options, object View)>();

		/// <summary>
		/// Counts the selections, so the plugin found for a log no longer selected is ignored.
		/// </summary>
		private int _selection;

		/// <param name="weevilOptions">Weevil's options, shown unless the log's plugin supplies its own.</param>
		/// <param name="weevilView">What the dialog shows for <paramref name="weevilOptions"/>.</param>
		/// <param name="findPlugin">Finds the plugin of a log that supplies its own options, off the UI thread, or returns <see langword="null"/>.</param>
		public OpenAsOptionsHost(IOpenAsOptions weevilOptions, object weevilView, Func<BrowserItem, IOpenAsOptionsPlugin> findPlugin)
		{
			_weevilOptions = weevilOptions ?? throw new ArgumentNullException(nameof(weevilOptions));
			_weevilView = weevilView;
			_findPlugin = findPlugin ?? throw new ArgumentNullException(nameof(findPlugin));

			this.Options = weevilOptions;
			this.View = weevilView;
			Watch(weevilOptions);
		}

		/// <summary>
		/// Raised on the UI thread when other options are shown, or when whether they can open may have changed.
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

		public bool CanOpen => this.Options.CanOpen;

		/// <summary>
		/// Shows the options of the log's plugin, or Weevil's, for the item selected: a log, or <see langword="null"/> when a folder, a zip or nothing is selected.
		/// </summary>
		/// <remarks>
		/// The plugin is found off the UI thread, since a plugin may read the log to tell whether it is one of its logs.
		/// </remarks>
		public async Task SelectAsync(BrowserItem file)
		{
			var selection = ++_selection;

			IOpenAsOptionsPlugin plugin = file is { IsFile: true }
				? await Task.Run(() => FindPlugin(file))
				: null;

			if (selection != _selection)
			{
				return;
			}

			Show(plugin);
			await this.Options.SelectAsync(file);

			// For options that don't say when they change.
			this.Changed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// The plugin of the log, or none when it can't be found: browsing must never stop Weevil.
		/// </summary>
		private IOpenAsOptionsPlugin FindPlugin(BrowserItem file)
		{
			try
			{
				return _findPlugin(file);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As could not find the plugin of a file, so it shows Weevil's options. File={file.Name}");
				return null;
			}
		}

		private void Show(IOpenAsOptionsPlugin plugin)
		{
			(IOpenAsOptions options, object view) = plugin is null ? (_weevilOptions, _weevilView) : GetOptions(plugin);

			if (ReferenceEquals(options, this.Options))
			{
				return;
			}

			Unwatch(this.Options);
			this.Options = options;
			this.View = view;
			Watch(options);

			this.Changed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// The plugin's options, created the first time, or Weevil's when the plugin fails to create them.
		/// </summary>
		private (IOpenAsOptions Options, object View) GetOptions(IOpenAsOptionsPlugin plugin)
		{
			if (!_pluginOptions.TryGetValue(plugin.GetType(), out (IOpenAsOptions Options, object View) created))
			{
				try
				{
					IOpenAsOptions options = plugin.CreateOpenAsOptions() ?? throw new InvalidOperationException("The plugin created no options.");
					created = (options, plugin.CreateOpenAsView(options));
				}
				catch (Exception e)
				{
					Log.Default.Write(LogSeverityType.Warning, e, $"A plugin could not create its Open As options, so Weevil's are shown. Plugin={plugin.GetType().FullName}");
					created = (_weevilOptions, _weevilView);
				}

				_pluginOptions[plugin.GetType()] = created;
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
				this.Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
