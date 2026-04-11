namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;

	internal class DragAndDropViewModel
	{
		private static readonly HashSet<string> DefaultCompatibleFiles = new HashSet<string>
		  {
				".LOG",
				".TSV",
				".CSV",
				".TXT",
				".ZIP",
		  };

		private readonly HashSet<string> _compatibleFileExtensions;

		public event EventHandler<DroppedFileEventArgs> DroppedFile;

		public DragAndDropViewModel() : this(DefaultCompatibleFiles)
		{
			// nothing to do
		}

		public DragAndDropViewModel(HashSet<string> compatibleFileExtensions)
		{
			_compatibleFileExtensions = new HashSet<string>();

			foreach (var extension in compatibleFileExtensions)
			{
				var standardizedExtension = string.Empty;

				if (!extension.StartsWith(".", System.StringComparison.InvariantCulture))
				{
					standardizedExtension = ".";
				}

				standardizedExtension += extension.Trim().ToUpperInvariant();

				_compatibleFileExtensions.Add(standardizedExtension);
			}
		}

		public void DragOver(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[])e.Data.GetData(DataFormats.FileDrop);
				e.Effects = files.Any(IsFileSupported) ? DragDropEffects.Copy : DragDropEffects.None;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		public void Drop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Any(IsFileSupported))
				{
					var filePath = files.First();
					DroppedFile?.Invoke(this, new DroppedFileEventArgs(filePath));
				}
			}
		}

		private bool IsFileSupported(string path)
		{
			var isCompatible = false;

			var extension = Path.GetExtension(path);
			if (!string.IsNullOrWhiteSpace(extension))
			{
				extension = extension.ToUpperInvariant();

				isCompatible = _compatibleFileExtensions.Contains(extension);
			}

			return isCompatible;
		}
	}
}
