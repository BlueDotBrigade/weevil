﻿namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Diagnostics;

	public class DetectUnresponsiveUiAnalyzer : IRecordAnalyzer
	{
		private static readonly TimeSpan DefaultUiResponsivenessPeriod = TimeSpan.FromSeconds(1);

		/// <summary>
		/// A user interface that is unresponsive for this length of time suggests
		/// that the application's business logic may be creeping into the user interface.
		/// </summary>
		/// <remarks>
		/// When this occurs, consider deferring processing to a background thread.
		/// </remarks>
		public static readonly TimeSpan Sluggish = TimeSpan.FromMilliseconds(250);

		/// <summary>
		/// A user interface that is unresponsive for this length of time
		/// indicates that there are serious problems that need to be addressed immediately.
		/// </summary>
		public static readonly TimeSpan NotResponsive = TimeSpan.FromMilliseconds(1000);

		public static readonly TimeSpan CutoffPeriod = TimeSpan.FromMinutes(15);

		private TimeSpan _maximumPeriodDetected;
		private int _problemsDetected;

		private readonly object _problemsDetectedPadlock;

		public DetectUnresponsiveUiAnalyzer()
		{
			_maximumPeriodDetected = TimeSpan.Zero;
			_problemsDetected = -1;

			_problemsDetectedPadlock = new object();
		}
		public string Key => AnalysisType.DetectUnresponsiveUi.ToString();

		public string DisplayName => "Detect Unresponsive UI";

		public TimeSpan MaximumPeriodDetected => _maximumPeriodDetected;

		public int UnresponsiveUiCount => _problemsDetected;

		/// <summary>
		/// Compares log file messages that were written by the UI thread, looking for unusually long gaps.
		/// </summary>
		/// <remarks>
		/// The analysis is performed using actual <see cref="Record.CreatedAt"/> timestamps, 
		/// and not the <see cref="Metadata.ElapsedTime"/> value which is generated by the current view.
		/// </remarks>
		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog)
		{
			Log.Default.Write(LogSeverityType.Debug, "User interface responsivenesss is being analyzed...");

			if (TryGetTolerance(userDialog, out TimeSpan maximumAllowedPeriod))
			{
				IRecord previousRecord = records.FirstOrDefault(record => record.Metadata.WasGeneratedByUi);

				if (Record.IsDummyOrNull(previousRecord))
				{
					foreach (IRecord record in records)
					{
						record.Metadata.IsFlagged = false;
					}
				}
				else
				{
					foreach (IRecord currentRecord in records)
					{
						currentRecord.Metadata.IsFlagged = false;

						if (currentRecord.Metadata.WasGeneratedByUi)
						{
							TimeSpan timePeriodBetweenUiMessages = currentRecord.CreatedAt - previousRecord.CreatedAt;

							if (timePeriodBetweenUiMessages > CutoffPeriod)
							{
								// assume the application was restarted
							}
							else
							{
								_maximumPeriodDetected = timePeriodBetweenUiMessages > _maximumPeriodDetected ? timePeriodBetweenUiMessages : _maximumPeriodDetected;

								if (timePeriodBetweenUiMessages > maximumAllowedPeriod)
								{
									Log.Default.Write(LogSeverityType.Warning, $"User interface appears to be unresponsive. Line={currentRecord.LineNumber}, Delay={timePeriodBetweenUiMessages}");
									_problemsDetected++;
									currentRecord.Metadata.IsFlagged = true;

									currentRecord.Metadata.UpdateUserComment($"Unresponsive UI: {TimeSpanExtensions.ToHumanReadable(timePeriodBetweenUiMessages)}");
								}
							}

							previousRecord = currentRecord;
						}
					}

					LogSeverityType severityType = _problemsDetected > 0 ? LogSeverityType.Warning : LogSeverityType.Information;
					Log.Default.Write(severityType, $"User interface responsivenesss analysis is complete. ProblemsDetected={_problemsDetected}, MaximumPeriodDetected={_maximumPeriodDetected}, MaximumAllowedPeriod={maximumAllowedPeriod}");
				}
			}

			return this.UnresponsiveUiCount;
		}

		private static bool TryGetTolerance(IUserDialog user, out TimeSpan unresponsivenessPeriod)
		{
			var userInput = user.ShowUserPrompt(
				"Input Required",
				"Maximum delay (ms):",
				DefaultUiResponsivenessPeriod.TotalMilliseconds.ToString("0.#"));

			var wasSuccessful = int.TryParse(userInput, out var timePeriodInMs);

			unresponsivenessPeriod = wasSuccessful ? TimeSpan.FromMilliseconds(timePeriodInMs) : TimeSpan.Zero;

			return wasSuccessful;
		}
	}
}