namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Globalization;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using Metalama.Patterns.Observability;

	/// <summary>
	/// Weevil's options in the Open As dialog: the selected log's details, then RAM or Disk with its advice and warnings (#1215).
	/// </summary>
	/// <remarks>
	/// Weevil suggests a mode for each log selected. Once the analyst picks one, selecting another log does not change it (decision 7 of the #1157 plan).
	/// </remarks>
	[Observable]
	internal sealed partial class OpenAsOptionsViewModel
	{
		private const string NoFileDetails = "Select a log to see its details.";

		private const string DefaultRamDescription = "Faster. Keeps the whole log in memory.";

		private readonly Func<BrowserItem, OpenAsAdvice> _readAdvice;

		/// <summary>
		/// Counts the selections, so the advice for a log no longer selected is thrown away.
		/// </summary>
		private int _selection;

		/// <param name="readAdvice">Works out the advice for a file, off the UI thread: <see cref="OpenAsAdvice.Read(BrowserItem)"/>.</param>
		public OpenAsOptionsViewModel(Func<BrowserItem, OpenAsAdvice> readAdvice)
		{
			_readAdvice = readAdvice ?? throw new ArgumentNullException(nameof(readAdvice));
		}

		/// <summary>
		/// The file selected, or <see langword="null"/> when a folder, a zip or nothing is selected.
		/// </summary>
		public BrowserItem File { get; private set; }

		/// <summary>
		/// The advice for <see cref="File"/>, or <see langword="null"/> while it is being worked out.
		/// </summary>
		public OpenAsAdvice Advice { get; private set; }

		public StorageMode StorageMode { get; private set; } = StorageMode.Ram;

		/// <summary>
		/// The analyst picked <see cref="StorageMode"/>, so Weevil no longer suggests one.
		/// </summary>
		public bool IsModeChosen { get; private set; }

		public bool HasFile => this.File is not null;

		public string Details { get; private set; } = NoFileDetails;

		public bool IsRamChosen
		{
			get => this.StorageMode == StorageMode.Ram;
			set
			{
				// Only the radio button checked is a choice: the other is unchecked as a consequence.
				if (value)
				{
					Choose(StorageMode.Ram);
				}
			}
		}

		public bool IsDiskChosen
		{
			get => this.StorageMode == StorageMode.Disk;
			set
			{
				if (value)
				{
					Choose(StorageMode.Disk);
				}
			}
		}

		public string RamDescription { get; private set; } = DefaultRamDescription;

		public string DiskDescription => OpenAsAdvice.DiskDescription;

		public string Suggestion { get; private set; } = string.Empty;

		/// <summary>
		/// The warning for the mode chosen, or an empty string. It never stops the open.
		/// </summary>
		public string Warning { get; private set; } = string.Empty;

		/// <summary>
		/// A log is selected and its advice is ready, so the mode is the one suggested for it, or the one the analyst picked.
		/// </summary>
		public bool CanOpen => this.Advice is not null;

		/// <summary>
		/// Shows the details and advice of the file selected, or none when a folder, a zip or nothing is selected.
		/// </summary>
		/// <remarks>
		/// The advice is worked out off the UI thread. If another file is selected meanwhile, it is thrown away.
		/// </remarks>
		public async Task SelectAsync(BrowserItem file)
		{
			var selection = ++_selection;

			this.File = file is { IsFile: true } ? file : null;
			this.Details = this.File is null ? NoFileDetails : Describe(this.File);
			ShowAdvice(null);

			if (this.File is null)
			{
				return;
			}

			OpenAsAdvice advice = await Task.Run(() => _readAdvice(file));

			if (selection != _selection)
			{
				return;
			}

			if (!this.IsModeChosen)
			{
				this.StorageMode = advice.DefaultMode;
			}

			ShowAdvice(advice);
		}

		private void Choose(StorageMode storageMode)
		{
			this.IsModeChosen = true;
			this.StorageMode = storageMode;
			this.Warning = this.Advice?.GetWarning(storageMode) ?? string.Empty;
		}

		/// <summary>
		/// Shows the advice for the log selected, or none while it is worked out.
		/// </summary>
		/// <remarks>
		/// The texts are set here, rather than worked out by their properties, because Metalama cannot see when a method's result changes.
		/// </remarks>
		private void ShowAdvice(OpenAsAdvice advice)
		{
			this.Advice = advice;
			this.RamDescription = advice?.RamDescription ?? DefaultRamDescription;
			this.Suggestion = advice is null ? string.Empty : $"Weevil suggests {(advice.DefaultMode == StorageMode.Disk ? "Disk" : "RAM")} for this log.";
			this.Warning = advice?.GetWarning(this.StorageMode) ?? string.Empty;
		}

		private static string Describe(BrowserItem file)
		{
			var size = file.Size is long bytes ? OpenAsAdvice.Format(new StorageUnit(bytes)) : "Size unknown";
			var modified = file.Modified is DateTime date ? $", modified {date.ToString("g", CultureInfo.CurrentCulture)}" : string.Empty;
			var where = file.Location.IsInArchive ? "In a zip" : "In";
			var onlineOnly = file.IsOnlineOnly ? $"{Environment.NewLine}Online only: OneDrive downloads it when it is opened." : string.Empty;

			return $"{file.Name}{Environment.NewLine}{size}{modified}{Environment.NewLine}{where}: {file.Location.GetParent()}{onlineOnly}";
		}
	}
}
