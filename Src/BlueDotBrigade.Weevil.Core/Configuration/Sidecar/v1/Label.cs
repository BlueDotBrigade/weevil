namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v1
{
	using System;

	public class Label : IDisposable
	{
		public Label()
		{
			// nothing to do
		}

		public string Name { get; set; }
		public int LineNumber { get; set; }


		private void ReleaseUnmanagedResources()
		{
			// No unmanaged resources to release.
		}

		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

		~Label()
		{
			ReleaseUnmanagedResources();
		}
	}
}
