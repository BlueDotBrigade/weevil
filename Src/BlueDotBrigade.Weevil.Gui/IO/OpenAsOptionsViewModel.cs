namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using Metalama.Patterns.Observability;

	/// <summary>
	/// Weevil's options in the Open As dialog: the selected log's details, the part of it to open (#1214), then RAM or Disk with its advice and warnings (#1215).
	/// </summary>
	/// <remarks>
	/// Weevil suggests a mode for each log selected. Once the analyst picks one, selecting another log does not change it (decision 7 of the #1157 plan).
	/// The part is the whole log, one section, one region or a range of lines (decision 5). Sections and regions belong to the log, so selecting another log
	/// goes back to the whole log, while a range of lines stays.
	/// </remarks>
	[Observable]
	internal sealed partial class OpenAsOptionsViewModel
	{
		private const string NoFileDetails = "Select a log to see its details.";

		private const string DefaultRamDescription = "Faster. Keeps the whole log in memory.";

		private readonly Func<BrowserItem, OpenAsAdvice> _readAdvice;

		private readonly Func<BrowserItem, IReadOnlyList<LogPart>> _readParts;

		/// <summary>
		/// Counts the selections, so the advice for a log no longer selected is thrown away.
		/// </summary>
		private int _selection;

		private IReadOnlyList<LogPart> _sections = Array.Empty<LogPart>();

		private IReadOnlyList<LogPart> _regions = Array.Empty<LogPart>();

		/// <param name="readAdvice">Works out the advice for a file, off the UI thread: <see cref="OpenAsAdvice.Read(BrowserItem)"/>.</param>
		/// <param name="readParts">Lists the sections and regions of a file that can be opened on their own, off the UI thread: none when omitted.</param>
		public OpenAsOptionsViewModel(Func<BrowserItem, OpenAsAdvice> readAdvice, Func<BrowserItem, IReadOnlyList<LogPart>> readParts = null)
		{
			_readAdvice = readAdvice ?? throw new ArgumentNullException(nameof(readAdvice));
			_readParts = readParts ?? (_ => Array.Empty<LogPart>());
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

		/// <summary>
		/// The mode the radio buttons show and set: setting it is the analyst's pick.
		/// </summary>
		/// <remarks>
		/// One property for both radio buttons, bound through <c>EnumToBooleanConverter</c>, which writes nothing when a button is unchecked.
		/// A <see langword="bool"/> per button would not do: Metalama raises <c>PropertyChanged</c> when the button unchecked writes <see langword="false"/>
		/// to a property that still reads <see langword="true"/>, and WPF would then check that button again, losing the click.
		/// </remarks>
		public StorageMode ChosenMode
		{
			get => this.StorageMode;
			set => Choose(value);
		}

		public string RamDescription { get; private set; } = DefaultRamDescription;

		public string DiskDescription => OpenAsAdvice.DiskDescription;

		public string Suggestion { get; private set; } = string.Empty;

		/// <summary>
		/// The warning for the mode chosen, or an empty string. It never stops the open.
		/// </summary>
		public string Warning { get; private set; } = string.Empty;

		/// <summary>
		/// How much of the log is opened.
		/// </summary>
		public PartKind PartKind { get; private set; } = PartKind.Whole;

		/// <summary>
		/// The part the radio buttons show and set, bound as <see cref="ChosenMode"/> is.
		/// </summary>
		public PartKind ChosenPart
		{
			get => this.PartKind;
			set
			{
				this.PartKind = value;
				UpdatePart();
			}
		}

		/// <summary>
		/// The log's level-1 sections, as the list shows them.
		/// </summary>
		public IReadOnlyList<string> Sections { get; private set; } = Array.Empty<string>();

		/// <summary>
		/// The log's named regions, as the list shows them.
		/// </summary>
		public IReadOnlyList<string> Regions { get; private set; } = Array.Empty<string>();

		/// <remarks>
		/// Set with <see cref="Sections"/>: Metalama can't see a list's <c>Count</c> change, so a property worked out from it would never be raised.
		/// </remarks>
		public bool HasSections { get; private set; }

		/// <remarks>
		/// Set with <see cref="Regions"/>, as <see cref="HasSections"/> is.
		/// </remarks>
		public bool HasRegions { get; private set; }

		/// <summary>
		/// The position in <see cref="Sections"/> of the section to open.
		/// </summary>
		public int SectionIndex { get; private set; } = -1;

		/// <summary>
		/// The section the list shows and sets: picking one opens that section.
		/// </summary>
		public int ChosenSectionIndex
		{
			get => this.SectionIndex;
			set
			{
				// WPF clears the list's selection when the list is replaced, which is not the analyst's pick.
				if (value >= 0)
				{
					this.SectionIndex = value;
					Choose(PartKind.Section);
				}
			}
		}

		/// <summary>
		/// The position in <see cref="Regions"/> of the region to open.
		/// </summary>
		public int RegionIndex { get; private set; } = -1;

		/// <summary>
		/// The region the list shows and sets: picking one opens that region.
		/// </summary>
		public int ChosenRegionIndex
		{
			get => this.RegionIndex;
			set
			{
				// WPF clears the list's selection when the list is replaced, which is not the analyst's pick.
				if (value >= 0)
				{
					this.RegionIndex = value;
					Choose(PartKind.Region);
				}
			}
		}

		public string FirstLineText { get; private set; } = string.Empty;

		/// <summary>
		/// The first line to open, as typed: typing it opens a range of lines.
		/// </summary>
		public string FirstLine
		{
			get => this.FirstLineText;
			set
			{
				this.FirstLineText = value ?? string.Empty;
				Choose(PartKind.Lines);
			}
		}

		public string LastLineText { get; private set; } = string.Empty;

		/// <summary>
		/// The last line to open, as typed, or nothing for the end of the log.
		/// </summary>
		public string LastLine
		{
			get => this.LastLineText;
			set
			{
				this.LastLineText = value ?? string.Empty;
				Choose(PartKind.Lines);
			}
		}

		/// <summary>
		/// The part of the log to open, or <see langword="null"/> when the lines typed are not a range, which <see cref="PartError"/> explains.
		/// </summary>
		public LogPart Part { get; private set; } = LogPart.Whole;

		/// <summary>
		/// Why the lines typed can't be opened, or an empty string.
		/// </summary>
		public string PartError { get; private set; } = string.Empty;

		/// <summary>
		/// A log is selected, its advice is ready and the part to open is valid. The mode is the one suggested for it, or the one the analyst picked.
		/// </summary>
		public bool CanOpen => this.Advice is not null && this.Part is not null;

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
			ShowParts(Array.Empty<LogPart>());

			if (this.File is null)
			{
				return;
			}

			// Read side by side: the parts may take longer, for example from a zip on a share, and the log can be opened whole meanwhile.
			Task<IReadOnlyList<LogPart>> readingParts = Task.Run(() => ReadParts(file));
			OpenAsAdvice advice = await Task.Run(() => ReadAdvice(file));

			if (selection != _selection)
			{
				return;
			}

			if (!this.IsModeChosen)
			{
				this.StorageMode = advice.DefaultMode;
			}

			ShowAdvice(advice);

			IReadOnlyList<LogPart> parts = await readingParts;

			if (selection == _selection)
			{
				ShowParts(parts);
			}
		}

		/// <summary>
		/// The parts of the file, or none when its sidecar can't be read: browsing must never stop Weevil.
		/// </summary>
		private IReadOnlyList<LogPart> ReadParts(BrowserItem file)
		{
			try
			{
				return _readParts(file);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As could not list the sections and regions of a file. File={file.Name}");
				return Array.Empty<LogPart>();
			}
		}

		/// <summary>
		/// Lists the sections and regions of the log selected, each with its first one chosen.
		/// <see cref="SelectAsync"/> lists none first, so the index always changes and the list shows it. A section or region chosen for the previous log
		/// goes back to the whole log; a range of lines stays.
		/// </summary>
		private void ShowParts(IReadOnlyList<LogPart> parts)
		{
			_sections = parts.Where(p => p.SectionName is not null).ToList();
			_regions = parts.Where(p => p.RegionName is not null).ToList();

			this.Sections = _sections.Select(p => Describe(p.SectionName, p)).ToList();
			this.Regions = _regions.Select(p => Describe(p.RegionName, p)).ToList();
			this.HasSections = _sections.Count > 0;
			this.HasRegions = _regions.Count > 0;
			this.SectionIndex = _sections.Count > 0 ? 0 : -1;
			this.RegionIndex = _regions.Count > 0 ? 0 : -1;

			if (this.PartKind is PartKind.Section or PartKind.Region)
			{
				this.PartKind = PartKind.Whole;
			}

			UpdatePart();
		}

		private void Choose(PartKind partKind)
		{
			this.PartKind = partKind;
			UpdatePart();
		}

		/// <summary>
		/// Works out the part to open from what the analyst chose.
		/// </summary>
		/// <remarks>
		/// Set here, rather than worked out by <see cref="Part"/>, because Metalama cannot see when a method's result changes.
		/// </remarks>
		private void UpdatePart()
		{
			(this.Part, this.PartError) = this.PartKind switch
			{
				PartKind.Section => (Find(_sections, this.SectionIndex), string.Empty),
				PartKind.Region => (Find(_regions, this.RegionIndex), string.Empty),
				PartKind.Lines => GetLines(this.FirstLineText, this.LastLineText),
				_ => (LogPart.Whole, string.Empty),
			};
		}

		/// <summary>
		/// The section or region at <paramref name="index"/>, or the whole log when the log has none.
		/// </summary>
		private static LogPart Find(IReadOnlyList<LogPart> parts, int index)
		{
			return index >= 0 && index < parts.Count ? parts[index] : LogPart.Whole;
		}

		/// <summary>
		/// The range of lines typed, or why it isn't one. An empty last line is the end of the log, so lines from 1 to the end are the whole log.
		/// </summary>
		internal static (LogPart Part, string Error) GetLines(string firstLine, string lastLine)
		{
			if (!TryParseLine(firstLine, out var first))
			{
				return (null, "Type the first line to open: a whole number from 1.");
			}

			if (string.IsNullOrWhiteSpace(lastLine))
			{
				return (LogPart.ForLines(first == 1 ? Range.All : new Range(first, int.MaxValue)), string.Empty);
			}

			if (!TryParseLine(lastLine, out var last))
			{
				return (null, "Type the last line as a whole number, or leave it empty to open to the end of the log.");
			}

			return last < first
				? (null, "The last line comes before the first.")
				: (LogPart.ForLines(new Range(first, last)), string.Empty);
		}

		private static bool TryParseLine(string text, out int lineNumber)
		{
			return int.TryParse(text?.Trim(), NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out lineNumber) && lineNumber >= 1;
		}

		/// <summary>
		/// A section or region as the list shows it: its name and its lines.
		/// </summary>
		private static string Describe(string name, LogPart part)
		{
			return part.IsToEndOfLog
				? $"{name} (from line {part.Lines.Start.Value:N0})"
				: $"{name} (lines {part.Lines.Start.Value:N0} – {part.Lines.End.Value:N0})";
		}

		/// <summary>
		/// The advice for the file, or advice as for a log that cannot be read: browsing must never stop Weevil.
		/// </summary>
		private OpenAsAdvice ReadAdvice(BrowserItem file)
		{
			try
			{
				return _readAdvice(file);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As could not work out its advice for a file. File={file.Name}");
				return new OpenAsAdvice(file.Name, StorageUnit.Zero, estimate: null, availableMemory: null, isOnNetworkDrive: false);
			}
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
