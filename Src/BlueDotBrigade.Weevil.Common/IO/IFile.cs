namespace BlueDotBrigade.Weevil.IO
{
	public interface IFile
	{
		bool Exists(string path);
		string ReadAllText(string path);
		System.IO.FileStream OpenRead(string path);

		void WriteAllText(string path, string content);

		void Delete(string path);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1704:Identifiers should be spelled correctly", Justification = "The name System.IO.File.Copy, which this wraps, gives the parameter.")]
		void Copy(string sourceFileName, string destFileName);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1704:Identifiers should be spelled correctly", Justification = "The name System.IO.File.Copy, which this wraps, gives the parameter.")]
		void Copy(string sourceFileName, string destFileName, bool overwrite);
	}
}