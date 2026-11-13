namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Hashing;
	using System.Linq;
	using System.Text;

	/// <summary>
	/// What parsed a log, and the checksums of the bytes it read, as a saved index keeps them (#1023).
	/// </summary>
	/// <remarks>
	/// A record table saved with the index is reused only when the parse that would make it again is the same:
	/// - the same plugin, in the same build, identified by its assembly's MVID, since every build of a release has the same version;
	/// - the same build of Weevil's Core, whose parser joins continuation lines.
	///   Builds are deterministic, so an MVID is a hash of the assembly's own compiled output: rebuilding without a change keeps it,
	///   but a change only to another assembly, such as Common or a plugin's dependency, does not change it;
	/// - the same storage mode, because some encodings keep strings in RAM and bytes on disk;
	/// - for a plugin with its own Open As window, the same context, in case its parser depends on it.
	/// The checksums say whether the log is still the one parsed.
	/// </remarks>
	internal sealed class SavedParse
	{
		public SavedParse(
			string pluginName,
			Guid pluginBuildId,
			Guid coreBuildId,
			StorageMode storageMode,
			ulong contextHash,
			long startOffset,
			long endOffset,
			ulong[] checksums)
		{
			this.PluginName = pluginName ?? string.Empty;
			this.PluginBuildId = pluginBuildId;
			this.CoreBuildId = coreBuildId;
			this.StorageMode = storageMode;
			this.ContextHash = contextHash;
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
			this.Checksums = checksums ?? throw new ArgumentNullException(nameof(checksums));
		}

		public string PluginName { get; }

		public Guid PluginBuildId { get; }

		public Guid CoreBuildId { get; }

		public StorageMode StorageMode { get; }

		/// <summary>
		/// A hash of the Open As context, or 0 when the plugin has no Open As window.
		/// </summary>
		public ulong ContextHash { get; }

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
		public static SavedParse Create(IPlugin plugin, StorageMode storageMode, ContextDictionary context, LogFileSource parsed)
		{
			ArgumentNullException.ThrowIfNull(plugin);
			ArgumentNullException.ThrowIfNull(parsed);

			return new SavedParse(
				plugin.Name,
				plugin.GetType().Assembly.ManifestModule.ModuleVersionId,
				typeof(SavedParse).Assembly.ManifestModule.ModuleVersionId,
				storageMode,
				plugin.CanOpenAs ? HashContext(context) : 0,
				parsed.StartOffset,
				parsed.EndOffset,
				parsed.GetChecksums().ToArray());
		}

		/// <summary>
		/// Returns a hash of the context's names and values that does not depend on their order.
		/// </summary>
		internal static ulong HashContext(ContextDictionary context)
		{
			if (context is null || context.Count == 0)
			{
				return 0;
			}

			var hash = new XxHash3();

			foreach (KeyValuePair<string, string> item in context.OrderBy(i => i.Key, StringComparer.Ordinal))
			{
				hash.Append(Encoding.UTF8.GetBytes(item.Key));
				hash.Append([0]);
				hash.Append(Encoding.UTF8.GetBytes(item.Value ?? string.Empty));
				hash.Append([0]);
			}

			return hash.GetCurrentHashAsUInt64();
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
			       this.StorageMode == other.StorageMode &&
			       this.ContextHash == other.ContextHash &&
			       this.StartOffset == other.StartOffset &&
			       this.EndOffset == other.EndOffset &&
			       this.Checksums.SequenceEqual(other.Checksums);
		}

		internal void Write(BinaryWriter writer)
		{
			writer.Write(this.PluginName);
			writer.Write(this.PluginBuildId.ToByteArray());
			writer.Write(this.CoreBuildId.ToByteArray());
			writer.Write((byte)this.StorageMode);
			writer.Write(this.ContextHash);
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
			var storageMode = (StorageMode)reader.ReadByte();
			var contextHash = reader.ReadUInt64();
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

			return new SavedParse(pluginName, pluginBuildId, coreBuildId, storageMode, contextHash, startOffset, endOffset, checksums);
		}
	}
}
