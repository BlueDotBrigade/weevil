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

        protected override void ProcessRecord()
        {
            try
            {
                IEngine engine = Engine
                    .UsingPath(this.FilePath)
                    .UsingStorage(this.Storage)
                    .Open();

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
