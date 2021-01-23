namespace BlueDotBrigade.Weevil.Analysis
{
	public enum FileFormatType
	{
		/// <summary>
		/// Indicates that the data should be written in the same format as the data source.
		/// </summary>
		Raw,
		/// <summary>
		/// Indicates that the data should be written in a Tab Separated Format (i.e. TSV)
		/// which can be imported into applications like Excel.
		/// </summary>
		Tsv,
	}
}
