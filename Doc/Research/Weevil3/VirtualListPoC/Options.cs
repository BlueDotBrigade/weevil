#nullable enable

namespace Weevil.VirtualListPoC;

using System.Globalization;

internal sealed class Options
{
	public const int DefaultRowCount = 100_000_000;

	public const int DefaultWpfSelectionRowCount = 1_000_000;

	public int RowCount { get; private set; } = DefaultRowCount;

	/// <summary>
	/// Uses WPF's own selection (<c>SelectionMode="Extended"</c>), as Weevil 2.12 does, instead of the range selection.
	/// </summary>
	public bool UsesWpfSelection { get; private set; }

	public bool RunsAutomatically { get; private set; }

	public string ReportPath { get; private set; } = "results.md";

	/// <summary>
	/// Keeps the window open after an automatic run.
	/// </summary>
	public bool Stays { get; private set; }

	public static string Help =>
		"Weevil.VirtualListPoC [options]\n\n" +
		"  --rows <1-300000000>  Rows in the list. Default 100,000,000, or 1,000,000 with --wpf-selection.\n" +
		"  --wpf-selection       Use WPF's own selection, as Weevil 2.12 does, to see where it breaks.\n" +
		"  --auto                Run every scenario, save the report, then close.\n" +
		"  --report <path>       Where --auto saves the report. Default results.md.\n" +
		"  --stay                Keep the window open after --auto.\n";

	public static Options Parse(string[] args)
	{
		var options = new Options();
		var rowsGiven = false;
		for (var i = 0; i < args.Length; i++)
		{
			switch (args[i].ToLowerInvariant())
			{
				case "--rows":
					if (!int.TryParse(Next(args, ref i).Replace(",", string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rows) ||
						rows < 1 || rows > SyntheticLog.MaximumRecordCount)
					{
						throw new ArgumentException("--rows must be from 1 to 300,000,000.");
					}

					options.RowCount = rows;
					rowsGiven = true;
					break;
				case "--wpf-selection":
					options.UsesWpfSelection = true;
					break;
				case "--auto":
					options.RunsAutomatically = true;
					break;
				case "--report":
					options.ReportPath = Next(args, ref i);
					break;
				case "--stay":
					options.Stays = true;
					break;
				default:
					throw new ArgumentException("Unknown option: " + args[i]);
			}
		}

		if (options.UsesWpfSelection && !rowsGiven)
		{
			options.RowCount = DefaultWpfSelectionRowCount;
		}

		return options;
	}

	private static string Next(string[] args, ref int i)
	{
		if (i + 1 >= args.Length)
		{
			throw new ArgumentException(args[i] + " needs a value.");
		}

		return args[++i];
	}
}
