namespace BlueDotBrigade.Weevil.PowerShell
{
	using System;
	using System.Management.Automation;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;

	/*
	# Assuming you have already loaded the BlueDotBrigade.Weevil.PowerShell module
	$logFilePath = "C:\Temp\Application.log"
	$records = Get-Records -FilePath $logFilePath -Include "Id=2" -Exclude "Error"
	$records | ForEach-Object { Write-Host $_ }

	# A log larger than memory
	$records = Get-Records -FilePath $logFilePath -Include "Id=2" -Storage Disk

	# Only one region, of those the log's sidecar lists
	$records = Get-Records -FilePath $logFilePath -Include "Id=2" -Region "Startup"

	# Only one level-1 section, with the sections nested in it
	$records = Get-Records -FilePath $logFilePath -Include "Id=2" -Section "Startup"
	*/
	[Cmdlet(VerbsCommon.Get, "Records")]
    public class GetRecordsCmdlet : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string FilePath { get; set; }

        [Parameter(Position = 1, Mandatory = false)]
        public FilterType FilterType { get; set; } = FilterType.RegularExpression;

        [Parameter(Position = 2, Mandatory = false)]
        public string Include { get; set; } = string.Empty;

        [Parameter(Position = 3, Mandatory = false)]
        public string Exclude { get; set; } = string.Empty;

        /// <summary>
        /// Ram, the default, keeps the log in memory. Disk reads it back from the log, for a log larger than memory (#1028).
        /// </summary>
        /// <remarks>
        /// On disk, each record reads its content from the log when the pipeline uses it, so a log that changes or is deleted before then fails those reads.
        /// </remarks>
        [Parameter(Mandatory = false, HelpMessage = "Ram keeps the log in memory. Disk reads it back from the log, for a log larger than memory.")]
        public StorageMode Storage { get; set; } = StorageMode.Ram;

        /// <summary>
        /// Opens only this region, one of those the log's sidecar lists (#1213). By default, the whole log is opened.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Opens only this region, one of those the log's sidecar lists.")]
        public string Region { get; set; }

        /// <summary>
        /// Opens only this level-1 section, one of those the log's sidecar lists, with the sections nested in it (#1212). By default, the whole log is opened.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Opens only this level-1 section, one of those the log's sidecar lists, with the sections nested in it.")]
        public string Section { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                IEngineBuilder builder = Engine
                    .UsingPath(this.FilePath)
                    .UsingStorage(this.Storage);

                builder = string.IsNullOrEmpty(this.Region) ? builder : builder.UsingRegion(this.Region);
                builder = string.IsNullOrEmpty(this.Section) ? builder : builder.UsingSection(this.Section);
                IEngine engine = builder.Open();

                var filterCriteria = new FilterCriteria(this.Include, this.Exclude);

                // On disk, a log that changed while it was filtered is reloaded, and filtered once more (#1028).
                engine.RunReloadingOnChange(current => current.Filter.Apply(this.FilterType, filterCriteria));

                // `Apply` returns the filter itself, so the matching records are read from its results (#1002).
                this.WriteObject(engine.Filter.Results, enumerateCollection: true);
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during processing
                this.WriteError(new ErrorRecord(ex, "FilterError", ErrorCategory.InvalidOperation, this));
            }
        }
    }
}
