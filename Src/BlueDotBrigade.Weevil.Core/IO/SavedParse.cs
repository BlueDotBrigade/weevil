namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// What parsed a log, and the checksums of the bytes it read, as a saved index keeps them (#1023).
	/// </summary>
	/// <remarks>
	/// A record table saved with the index is reused only when the parse that would make it again is the same:
	/// - the same plugin, in the same build, identified by its assembly's MVID, since every build of a release has the same version;
	/// - the same build of Weevil: of Core, whose parser joins continuation lines, and of Common, which holds the records and chunks the table is made of.
	///   Builds are deterministic, so an MVID is a hash of the assembly's own compiled output: rebuilding without a change keeps it;
	/// - the same storage mode, because some encodings keep strings in RAM and bytes on disk.
	/// The Open As context is not part of it: the sidecar keeps it, and it never reaches the parser (decided 2026-11-13).
	/// The checksums say whether the log is still the one parsed.
	/// </remarks>
	internal sealed class SavedParse
	{
		public SavedParse(
			string pluginName,
			Guid pluginBuildId,
			Guid coreBuildId,
			Guid commonBuildId,
			StorageMode storageMode,
			long startOffset,
			long endOffset,
			ulong[] checksums)
		{
			this.PluginName = pluginName ?? string.Empty;
			this.PluginBuildId = pluginBuildId;
			this.CoreBuildId = coreBuildId;
			this.CommonBuildId = commonBuildId;
			this.StorageMode = storageMode;
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
			this.Checksums = checksums ?? throw new ArgumentNullException(nameof(checksums));
		}

		public string PluginName { get; }

		public Guid PluginBuildId { get; }

		public Guid CoreBuildId { get; }

		public Guid CommonBuildId { get; }

		public StorageMode StorageMode { get; }

		/// <summary>
		/// Where the bytes parsed start in the log.
		/// </summary>
		public long StartOffset { get; }

		/// <summary>
		/// Where the bytes parsed end in the log.
		/// </summary>
		public long EndOffset { get; }

		/// <summary>
		/// The checksum of each block of the bytes parsed: see <see cref="LogFileSource.GetChecksums"/>.
		/// </summary>
		public IReadOnlyList<ulong> Checksums { get; }

		/// <summary>
		/// Describes a full parse: what parsed it, and the bytes it read.
		/// </summary>
		public static SavedParse Create(IPlugin plugin, StorageMode storageMode, LogFileSource parsed)
		{
			ArgumentNullException.ThrowIfNull(plugin);
			ArgumentNullException.ThrowIfNull(parsed);

			return new SavedParse(
				plugin.Name,
				GetBuildId(plugin.GetType()),
				GetBuildId(typeof(SavedParse)),
				GetBuildId(typeof(Record)),
				storageMode,
				parsed.StartOffset,
				parsed.EndOffset,
				parsed.GetChecksums().ToArray());
		}

		/// <summary>
		/// Whether a parse by <paramref name="plugin"/> in <paramref name="storageMode"/>, with this build of Weevil, would be this parse.
		/// </summary>
		public bool IsSameParser(IPlugin plugin, StorageMode storageMode)
		{
			ArgumentNullException.ThrowIfNull(plugin);

			return this.PluginName == (plugin.Name ?? string.Empty) &&
			       this.PluginBuildId == GetBuildId(plugin.GetType()) &&
			       this.CoreBuildId == GetBuildId(typeof(SavedParse)) &&
			       this.CommonBuildId == GetBuildId(typeof(Record)) &&
			       this.StorageMode == storageMode;
		}

		/// <summary>
		/// The same parse of the same bytes: every field is equal.
		/// </summary>
		public bool IsSameAs(SavedParse other)
		{
			return other != null &&
			       this.PluginName == other.PluginName &&
			       this.PluginBuildId == other.PluginBuildId &&
			       this.CoreBuildId == other.CoreBuildId &&
			       this.CommonBuildId == other.CommonBuildId &&
			       this.StorageMode == other.StorageMode &&
			       this.StartOffset == other.StartOffset &&
			       this.EndOffset == other.EndOffset &&
			       this.Checksums.SequenceEqual(other.Checksums);
		}

		internal void Write(BinaryWriter writer)
		{
			writer.Write(this.PluginName);
			writer.Write(this.PluginBuildId.ToByteArray());
			writer.Write(this.CoreBuildId.ToByteArray());
			writer.Write(this.CommonBuildId.ToByteArray());
			writer.Write((byte)this.StorageMode);
			writer.Write(this.StartOffset);
			writer.Write(this.EndOffset);
			writer.Write(this.Checksums.Count);

			foreach (var checksum in this.Checksums)
			{
				writer.Write(checksum);
			}
		}

		/// <returns>The parse, or <see langword="null"/> when what was read is inconsistent.</returns>
		/// <remarks>
		/// The number of checksums is checked against the range when a source is created from them: see <see cref="LogFileSource.Create"/>.
		/// </remarks>
		internal static SavedParse Read(IndexSection.Reader section, long logLength)
		{
			BinaryReader reader = section.Binary;

			var pluginName = reader.ReadString();
			var pluginBuildId = new Guid(reader.ReadBytes(16));
			var coreBuildId = new Guid(reader.ReadBytes(16));
			var commonBuildId = new Guid(reader.ReadBytes(16));
			var storageMode = (StorageMode)reader.ReadByte();
			var startOffset = reader.ReadInt64();
			var endOffset = reader.ReadInt64();
			var checksumCount = reader.ReadInt32();

			var isConsistent =
				Enum.IsDefined(storageMode) &&
				startOffset >= 0 &&
				endOffset >= startOffset &&
				endOffset <= logLength &&
				checksumCount >= 0 &&
				checksumCount * (long)sizeof(ulong) == section.Remaining;

			if (!isConsistent)
			{
				return null;
			}

			var checksums = new ulong[checksumCount];

			for (var i = 0; i < checksumCount; i++)
			{
				checksums[i] = reader.ReadUInt64();
			}

			return new SavedParse(pluginName, pluginBuildId, coreBuildId, commonBuildId, storageMode, startOffset, endOffset, checksums);
		}

		private static Guid GetBuildId(Type type)
		{
			return type.Assembly.ManifestModule.ModuleVersionId;
		}
	}
}
