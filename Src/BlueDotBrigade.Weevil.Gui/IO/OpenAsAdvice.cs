namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Globalization;
	using System.IO;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// What the Open As window offers for one log: the storage mode it suggests, and the warning for each choice (#1028).
	/// </summary>
	/// <remarks>
	/// RAM is suggested when the log's estimated memory fits in the available memory, less <see cref="Reserve"/>; otherwise Disk (decision 2 of the #1028 plan).
	/// When either figure is unknown, RAM is suggested, as Weevil opened every log before.
	/// </remarks>
	internal sealed class OpenAsAdvice
	{
		/// <summary>
		/// The memory kept free for Weevil's window and headroom.
		/// </summary>
		public static readonly StorageUnit Reserve = new StorageUnit(StorageUnit.BytesPerGigabyte);

		/// <param name="fileName">The log's name, as the window shows it.</param>
		/// <param name="fileSize">The log's size.</param>
		/// <param name="estimate">The memory the log needs in RAM (<see cref="Engine.EstimateMemory"/>), or <see langword="null"/> when it could not be estimated.</param>
		/// <param name="availableMemory">The computer's available physical memory, or <see langword="null"/> when it could not be read.</param>
		/// <param name="isOnNetworkDrive">The log is read over the network.</param>
		public OpenAsAdvice(string fileName, StorageUnit fileSize, StorageUnit? estimate, StorageUnit? availableMemory, bool isOnNetworkDrive)
		{
			this.FileName = fileName ?? string.Empty;
			this.FileSize = fileSize;
			this.Estimate = estimate;
			this.AvailableMemory = availableMemory;
			this.IsOnNetworkDrive = isOnNetworkDrive;
		}

		public string FileName { get; }

		public StorageUnit FileSize { get; }

		public StorageUnit? Estimate { get; }

		public StorageUnit? AvailableMemory { get; }

		public bool IsOnNetworkDrive { get; }

		/// <summary>
		/// The log is expected to fit in RAM, or it cannot be told.
		/// </summary>
		public bool FitsInRam =>
			this.Estimate is not { } estimate ||
			this.AvailableMemory is not { } available ||
			estimate.Bytes + Reserve.Bytes <= available.Bytes;

		public StorageMode DefaultMode => this.FitsInRam ? StorageMode.Ram : StorageMode.Disk;

		public string FileDescription => $"{this.FileName} ({Format(this.FileSize)})";

		public string RamDescription =>
			this.Estimate is { } estimate && this.AvailableMemory is { } available
				? $"Faster. Needs about {Format(estimate)} of the {Format(available)} free."
				: "Faster. Keeps the whole log in memory.";

		public static string DiskDescription => "Keeps about 22 bytes per record in memory, and reads the log each time it is needed.";

		/// <summary>
		/// The warning the window shows for <paramref name="storageMode"/>, or an empty string. A warning never stops the open.
		/// </summary>
		public string GetWarning(StorageMode storageMode)
		{
			if (storageMode == StorageMode.Ram && !this.FitsInRam)
			{
				return $"This log needs about {Format(this.Estimate.Value)} of memory, and {Format(this.AvailableMemory.Value)} is free. " +
				       "Weevil may become slow, or run out of memory. Disk keeps only the record table in memory.";
			}

			if (storageMode == StorageMode.Disk && this.IsOnNetworkDrive)
			{
				return "This log is on a network drive. On disk, every filter reads it over the network: expect it to be several times slower than on a local drive.";
			}

			return string.Empty;
		}

		/// <summary>
		/// Whether the log is read over the network: a UNC path, or a mapped network drive.
		/// </summary>
		/// <remarks>
		/// A zipped log is read from its copy in the temporary folder, so it is never on a network drive.
		/// </remarks>
		public static bool IsNetworkPath(string path)
		{
			try
			{
				if (path.StartsWith(@"\\", StringComparison.Ordinal))
				{
					return true;
				}

				var root = Path.GetPathRoot(path);

				return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
			}
			catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>
		/// A size as the window shows it: "1.2 GB", or "350 MB" below a gigabyte.
		/// </summary>
		public static string Format(StorageUnit size)
		{
			return size.GigaBytes >= 1
				? string.Format(CultureInfo.CurrentCulture, "{0:0.0} GB", size.GigaBytes)
				: string.Format(CultureInfo.CurrentCulture, "{0:0} MB", Math.Max(1, Math.Ceiling(size.MegaBytes)));
		}
	}
}
