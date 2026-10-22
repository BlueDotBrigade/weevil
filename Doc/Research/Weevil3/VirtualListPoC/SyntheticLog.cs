#nullable enable

namespace Weevil.VirtualListPoC;

using System.Globalization;
using System.Text;

internal enum SeverityType
{
	Debug,
	Information,
	Warning,
	Error,
	Critical,
}

internal enum FilterKind
{
	AllRecords,
	EverySecondRecord,
	EveryTenthRecord,
	WarningsAndAbove,
}

/// <summary>
/// A log of any size that exists only as arithmetic: each record is generated from its record number,
/// so 100 million rows need no file and no memory.
/// </summary>
/// <remarks>
/// Every 50th record is a 6-line call stack, every 20th is long enough to wrap, and every 1,000th
/// is longer than the grid's 8 KB limit, so rows have different heights, line numbers have gaps,
/// and the truncation tag appears.
/// </remarks>
internal static class SyntheticLog
{
	public const int MaximumRecordCount = 300_000_000;

	private const int CallStackPeriod = 50;
	private const int CallStackOffset = 25;
	private const int CallStackExtraLines = 5;

	private static readonly DateTime Start = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Unspecified);

	private static readonly string LongText = new string('x', 9_000);

	public static int GetLineNumber(int recordNumber)
	{
		// Each call stack before this record adds 5 lines: records 25, 75, 125, ...
		return recordNumber + 1 + (CallStackExtraLines * ((recordNumber + CallStackPeriod - 1 - CallStackOffset) / CallStackPeriod));
	}

	public static bool IsMultiLine(int recordNumber)
	{
		return recordNumber % CallStackPeriod == CallStackOffset;
	}

	public static SeverityType GetSeverity(int recordNumber)
	{
		if (IsMultiLine(recordNumber) || recordNumber % 97 == 0)
		{
			return recordNumber % 997 == 0 ? SeverityType.Critical : SeverityType.Error;
		}

		if (recordNumber % 89 == 0)
		{
			return SeverityType.Warning;
		}

		return recordNumber % 5 == 0 ? SeverityType.Debug : SeverityType.Information;
	}

	public static DateTime GetTimestamp(int recordNumber)
	{
		// About 10 ms apart, with a little jitter so the Elapsed column varies.
		return Start.AddTicks((recordNumber * 10L * TimeSpan.TicksPerMillisecond) + ((recordNumber * 7919L % 13) * TimeSpan.TicksPerMillisecond));
	}

	public static string GetContent(int recordNumber)
	{
		var timestamp = GetTimestamp(recordNumber).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		SeverityType severity = GetSeverity(recordNumber);
		var thread = (recordNumber % 7) + 1;

		if (IsMultiLine(recordNumber))
		{
			var builder = new StringBuilder(640);
			builder.Append(timestamp).Append('\t').Append(severity).Append('\t').Append("Thread=").Append(thread)
				.Append("\tRequest ").Append(recordNumber.ToString("N0", CultureInfo.InvariantCulture))
				.Append(" failed: System.InvalidOperationException: Sequence contains no elements\r\n")
				.Append("   at System.Linq.ThrowHelper.ThrowNoElementsException()\r\n")
				.Append("   at System.Linq.Enumerable.First[TSource](IEnumerable`1 source)\r\n")
				.Append(@"   at Contoso.Orders.OrderService.Submit(Order order) in C:\Src\Contoso\Orders\OrderService.cs:line 42").Append("\r\n")
				.Append(@"   at Contoso.Orders.OrdersController.Post(OrderDto dto) in C:\Src\Contoso\Orders\OrdersController.cs:line 18").Append("\r\n")
				.Append("   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.Execute(ObjectMethodExecutor executor, Object controller, Object[] arguments)");
			return builder.ToString();
		}

		var message = recordNumber % 1_000 == 500
			? "Payload=" + LongText
			: recordNumber % 20 == 7
				? "Retrying the request because the upstream service answered 503 Service Unavailable. The retry policy waits, doubles the delay, and gives up after five attempts; each attempt is logged with the correlation ID, the endpoint, the elapsed time, and the response headers so that a long line wraps in the grid."
				: "Request completed in " + (recordNumber % 500).ToString(CultureInfo.InvariantCulture) + " ms";

		return timestamp + "\t" + severity + "\tThread=" + thread.ToString(CultureInfo.InvariantCulture)
			+ "\tRequest " + recordNumber.ToString("N0", CultureInfo.InvariantCulture) + ": " + message;
	}

	public static bool IsMatch(FilterKind filter, int recordNumber)
	{
		return filter switch
		{
			FilterKind.AllRecords => true,
			FilterKind.EverySecondRecord => recordNumber % 2 == 0,
			FilterKind.EveryTenthRecord => recordNumber % 10 == 0,
			FilterKind.WarningsAndAbove => GetSeverity(recordNumber) >= SeverityType.Warning,
			_ => throw new ArgumentOutOfRangeException(nameof(filter)),
		};
	}

	/// <summary>
	/// Returns the record numbers that match, in order: 4 bytes per result, as in the Weevil 3.0 design.
	/// </summary>
	public static int[] Filter(FilterKind filter, int recordCount, CancellationToken cancellationToken = default)
	{
		if (filter == FilterKind.AllRecords)
		{
			var all = new int[recordCount];
			for (var i = 0; i < all.Length; i++)
			{
				all[i] = i;
			}

			return all;
		}

		var count = 0;
		for (var i = 0; i < recordCount; i++)
		{
			if (IsMatch(filter, i))
			{
				count++;
			}

			if ((i & 0xFFFFF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		var results = new int[count];
		var next = 0;
		for (var i = 0; i < recordCount && next < count; i++)
		{
			if (IsMatch(filter, i))
			{
				results[next++] = i;
			}
		}

		return results;
	}

	public static string Describe(FilterKind filter)
	{
		return filter switch
		{
			FilterKind.AllRecords => "All records",
			FilterKind.EverySecondRecord => "Every 2nd record",
			FilterKind.EveryTenthRecord => "Every 10th record",
			FilterKind.WarningsAndAbove => "Warnings and above",
			_ => filter.ToString(),
		};
	}
}
