namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;

	public interface IDirectory
	{
		bool Exists(string path);
		string[] GetDirectories(string path);
		string[] GetFiles(string path);
		FileInfo[] GetFiles(string path, string searchPattern, SearchOption searchOption);
	}
}