namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using GongSolutions.Wpf.DragDrop;

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

		public void Drag(IDropInfo dropInfo)
		{
			IEnumerable<string> dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
			dropInfo.Effects = dragFileList.Any(IsFileSupported) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		public void Drop(IDropInfo dropInfo)
		{
			var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>().ToList();
			dropInfo.Effects = dragFileList.Any(IsFileSupported) ? DragDropEffects.Copy : DragDropEffects.None;

			if (dropInfo.Effects == DragDropEffects.Copy)
			{
				var filePath = dragFileList.First();
				DroppedFile?.Invoke(this, new DroppedFileEventArgs(filePath));
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
