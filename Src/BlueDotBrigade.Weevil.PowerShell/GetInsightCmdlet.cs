namespace BlueDotBrigade.Weevil.PowerShell
{
	using System;
	using System.Collections.Generic;
	using System.Management.Automation;
	using BlueDotBrigade.Weevil.Analysis;

	/*
	# Assuming you have already loaded the BlueDotBrigade.Weevil.PowerShell module
	$logFilePath = "C:\Temp\Application.log"

	$insights = Get-Insight -FilePath $logFilePath

	$insights | ForEach-Object {
		Write-Host "Title: $_.Title"
		Write-Host "Metric: $_.MetricValue $_.MetricUnit"
		Write-Host "Details: $_.Details"
		Write-Host "Attention Required: $_.IsAttentionRequired"
		Write-Host "-------------------------------------"
	}
	*/
	[Cmdlet(VerbsCommon.Get, "Insight")]
    public class GetInsightCmdlet : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string FilePath { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                IEngine engine = Engine
                    .UsingPath(this.FilePath)
                    .Open();

                IEnumerable<IInsight> insights = engine.Analyzer.GetInsights();

                foreach (var insight in insights)
                {
                    this.WriteObject(insight);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during processing
                this.WriteError(new ErrorRecord(ex, "InsightError", ErrorCategory.InvalidOperation, this));
            }
        }
    }
}
