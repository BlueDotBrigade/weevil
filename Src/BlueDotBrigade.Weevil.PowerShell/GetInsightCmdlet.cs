namespace BlueDotBrigade.Weevil.PowerShell
{
	using System;
	using System.Collections.Immutable;
	using System.Management.Automation;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	/*
	# Assuming you have already loaded the BlueDotBrigade.Weevil.PowerShell module
	$report = Get-Insight -FilePath "C:\Temp\Application.log"

	$report.Metrics | Format-Table Name, Value

	$report.Insights | Where-Object IsAttentionRequired | ForEach-Object {
		Write-Host "$($_.Title): $($_.MetricValue) $($_.MetricUnit)"
		Write-Host $_.Details
	}
	*/
	/// <summary>
	/// Writes the log's <see cref="InsightReport"/>: the same data as the command line's <c>insight --output-as json</c>, as an object (#837).
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "Insight")]
    [OutputType(typeof(InsightReport))]
    public class GetInsightCmdlet : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string FilePath { get; set; }

        /// <summary>
        /// Ram, the default, keeps the log in memory. Disk reads it back from the log, for a log larger than memory (#1028).
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Ram keeps the log in memory. Disk reads it back from the log, for a log larger than memory.")]
        public StorageMode Storage { get; set; } = StorageMode.Ram;

        protected override void ProcessRecord()
        {
            try
            {
                IEngine engine = Engine
                    .UsingPath(this.FilePath)
                    .UsingStorage(this.Storage)
                    .Open();

                // On disk, a log that changed while it was read is reloaded, and read once more (#1028).
                ImmutableArray<IInsight> insights = engine.RunReloadingOnChange(current => current.Analyzer.GetInsights());

                // After the insights, so that the report's metrics describe the same log, if it was reloaded.
                var period = engine.Records.GetEstimatedRange();
                var version = typeof(GetInsightCmdlet).Assembly.GetName().Version ?? new Version(0, 0);

                this.WriteObject(new InsightReportGenerator().Create(version, engine, insights, period.From, period.To));
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during processing
                this.WriteError(new ErrorRecord(ex, "InsightError", ErrorCategory.InvalidOperation, this));
            }
        }
    }
}
