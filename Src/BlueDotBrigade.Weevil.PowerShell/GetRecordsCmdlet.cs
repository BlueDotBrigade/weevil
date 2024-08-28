using System;
using System.Management.Automation;
using BlueDotBrigade.Weevil.Core;
using BlueDotBrigade.Weevil.Core.Filtering;

namespace BlueDotBrigade.Weevil.PowerShell
{
	/*
	# Assuming you have already loaded the BlueDotBrigade.Weevil.PowerShell module
	$logFilePath = "C:\Temp\Application.log"
	$records = Get-Records -FilePath $logFilePath -Include "Id=2" -Exclude "Error"
	$records | ForEach-Object { Write-Host $_ }
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

        protected override void ProcessRecord()
        {
            try
            {
                IEngine engine = Engine
                    .UsingPath(this.FilePath)
                    .Open();

                var filterCriteria = new FilterCriteria(this.Include, this.Exclude);

                var filteredEntries = engine.Filter.Apply(this.FilterType, filterCriteria);

                this.WriteObject(filteredEntries, enumerateCollection: true);
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during processing
                this.WriteError(new ErrorRecord(ex, "FilterError", ErrorCategory.InvalidOperation, this));
            }
        }
    }
}
