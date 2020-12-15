namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;

	public class File : IFile
	{
		public virtual bool Exists(string path)
		{
			return System.IO.File.Exists(path);
		}

		public virtual string ReadAllText(string path)
		{
			return System.IO.File.ReadAllText(path);
		}

		public virtual FileStream OpenRead(string path)
		{
			return System.IO.File.OpenRead(path);
		}

		public virtual void WriteAllText(string path, string content)
		{
			System.IO.File.WriteAllText(path, content);
		}

		public virtual void Delete(string path)
		{
			System.IO.File.Delete(path);
		}

		public virtual void Copy(string sourceFileName, string destFileName)
		{
			System.IO.File.Copy(sourceFileName, destFileName);
		}

		public virtual void Copy(string sourceFileName, string destFileName, bool overwrite)
		{
			System.IO.File.Copy(sourceFileName, destFileName, overwrite);
		}
	}
}