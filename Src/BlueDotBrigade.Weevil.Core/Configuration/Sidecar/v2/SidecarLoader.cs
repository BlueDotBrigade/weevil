namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Runtime.Serialization;
	using Data;
	using Diagnostics;
	using IO;
	using Navigation;
	using BlueDotBrigade.Weevil.Collections.Generic;
	using Runtime.Serialization;

	internal class SidecarLoader
	{
		private readonly IFile _file;
		private readonly string _filePath;

		private WeevilSidecar _sidecar;

		public SidecarLoader(string filePath)
		{
			_file = new File();
			_filePath = filePath;
		}

		public bool Load()
		{
			return TryLoad(out _sidecar);
		}

		internal bool TryLoad(out WeevilSidecar sidecarData)
		{
			var canLoad = false;
			sidecarData = new WeevilSidecar();

			try
			{
				if (_file.Exists(_filePath))
				{
					sidecarData = TypeFactory.LoadFromXml<WeevilSidecar>(_filePath);
					canLoad = true;
				}
			}
			catch (SerializationException e)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					e,
					"Sidecar data could not be loaded. The file format may not be compatible with this version of Weevil.");
			}

			return canLoad;
		}

		public void Load(ImmutableArray<IRecord> records, Dictionary<string, string> fileParserConfiguration, out string sourceFileRemarks, List<string> inclusiveFilterHistory, List<string> exclusiveFilterHistory, List<Section> tableOfContents, List<Region> regions)
		{
			sourceFileRemarks = _sidecar?.CommonData?.UserRemarks;

			if (_sidecar.CommonData.Records != null)
			{
				foreach (RecordInfo recordInfo in _sidecar.CommonData.Records)
				{
					if (records.TryRecordOfLineNumber(recordInfo.RelatesTo.LineNumber, out IRecord record))
					{
						if (!string.IsNullOrWhiteSpace(record.Metadata.Comment))
						{
							Log.Default.Write(
								LogSeverityType.Warning,
								$"Overwriting current user comment with the value found in the sidecar. LineNumber={recordInfo.RelatesTo.LineNumber}");
						}

						record.Metadata.Comment = recordInfo.Comment;
						record.Metadata.IsPinned = recordInfo.IsPinned;
					}
					else
					{
						Log.Default.Write(
							LogSeverityType.Warning,
							$"Sidecar references a record that cannot be found in this log file. LineNumber={recordInfo.RelatesTo.LineNumber}");
					}
				}
			}

			if (_sidecar.CommonData.Context != null)
			{
				fileParserConfiguration.AddRange(_sidecar.CommonData.Context);
			}

			if (_sidecar.CommonData != null)
			{
				if (_sidecar?.CommonData.FilterHistory != null)
				{
					if (_sidecar?.CommonData.FilterHistory.InclusiveFilters != null)
					{
						inclusiveFilterHistory.AddRange(_sidecar.CommonData.FilterHistory.InclusiveFilters
							.OrderBy(x => x.SortOrder).Select(x => x.Value));
					}

					if (_sidecar?.CommonData.FilterHistory.ExclusiveFilters != null)
					{
						exclusiveFilterHistory.AddRange(_sidecar.CommonData.FilterHistory.ExclusiveFilters
							.OrderBy(x => x.SortOrder).Select(x => x.Value));

					}
				}
			}

			if (_sidecar.CommonData.TableOfContents != null)
			{
				foreach (SectionInfo sectionInfo in _sidecar.CommonData.TableOfContents)
				{
					tableOfContents.Add((new Section
					{
						Name = sectionInfo.Name,
						Level = sectionInfo.Level,
						ByteOffset = sectionInfo.RelatesTo.ByteOffset,
						LineNumber = sectionInfo.RelatesTo.LineNumber,
					}));
				}
			}

			if (_sidecar.CommonData.Regions != null)
			{
				foreach (RegionInfo regionInfo in _sidecar.CommonData.Regions)
				{
					regions.Add(
						new Region(
							regionInfo.Name,
							regionInfo.Minimum,
							regionInfo.Maximum));
				}
			}
		}

		public void Save(SidecarData newData, bool deleteBackup)
		{
			var backupFilePath = $"{_filePath}~";

			Log.Default.Write(LogSeverityType.Debug, $"Sidecar data is being saved... File={_filePath}");

			TryLoad(out WeevilSidecar oldData);

			WeevilSidecar mergedSidecar = PackSidecar(oldData, newData);

			Serialize(mergedSidecar, deleteBackup, backupFilePath);

			Log.Default.Write(LogSeverityType.Information, $"Sidecar data has been saved. File={_filePath}");
		}

		private void Serialize(WeevilSidecar sidecar, bool deleteBackup, string backupFilePath)
		{
			sidecar.Header.SchemaVersion = new Version(4, 1);
			sidecar.Header.SavedAt = DateTime.Now;

			TypeFactory.SaveAsXml(sidecar, _filePath);

			if (deleteBackup)
			{
				if (_file.Exists(backupFilePath))
				{
					_file.Delete(backupFilePath);
				}
			}
		}

		private static RecordInfo ToRecordInfo(IRecord record)
		{
			return new RecordInfo()
			{
				Id = record.LineNumber,
				RelatesTo = new RelatesTo
				{
					LineNumber = record.LineNumber,
				},
				Comment = record.Metadata.Comment,
				IsPinned = record.Metadata.IsPinned,
			};
		}

		private static WeevilSidecar PackSidecar(WeevilSidecar oldData, SidecarData newData)
		{
			var snapshot = new WeevilSidecar();
			
			var masterRecords = new Dictionary<int, RecordInfo>();

			// Is there any data from the past that we need to keep?
			if (oldData.CommonData?.Records.Count > 0)
			{
				// No new data?
				// ... then just save what we had before
				// ... otherwise merge the old with the new
				if (newData.Records.Length == 0)
				{
					var oldRecords = oldData.CommonData.Records
						.ToDictionary(k => k.RelatesTo.LineNumber, v => v);
					masterRecords.AddRange(oldRecords);
				}
				else
				{
					foreach (RecordInfo oldRecord in oldData.CommonData.Records)
					{
						if (newData.Records.TryRecordOfLineNumber(oldRecord.RelatesTo.LineNumber, out IRecord newRecord))
						{
							if (!string.IsNullOrWhiteSpace(newRecord.Metadata.Comment) ||
								newRecord.Metadata.IsPinned)
							{
								masterRecords.Add(newRecord.LineNumber, ToRecordInfo(newRecord));
							}
						}
						else
						{
							masterRecords.Add(oldRecord.RelatesTo.LineNumber, oldRecord);
						}
					}
				}
			}

			// Find new data that needs to be saved.
			if (newData.Records.Length > 0)
			{
				foreach (IRecord newRecord in newData.Records)
				{
					if (!string.IsNullOrWhiteSpace(newRecord.Metadata.Comment) ||
						newRecord.Metadata.IsPinned)
					{
						if (!masterRecords.ContainsKey(newRecord.LineNumber))
						{
							masterRecords.Add(newRecord.LineNumber, ToRecordInfo(newRecord));
						}
					}
				}
			}

			snapshot
				.CommonData
				.Records
				.AddRange(masterRecords.Select(x => x.Value));

			if (newData.Context != null)
			{
				snapshot.CommonData.Context.AddRange(newData.Context);
			}

			if (newData.FilterTraits?.IncludeHistory != null)
			{
				snapshot.CommonData.FilterHistory.InclusiveFilters =
					GetFilterHistorySnapshot(newData.FilterTraits.IncludeHistory);
			}

			if (newData.FilterTraits?.ExcludeHistory != null)
			{
				snapshot.CommonData.FilterHistory.ExclusiveFilters =
					GetFilterHistorySnapshot(newData.FilterTraits.ExcludeHistory);
			}

			if (newData.TableOfContents?.Sections != null)
			{
				foreach (Section section in newData.TableOfContents.Sections)
				{
					snapshot.CommonData.TableOfContents.Add(new SectionInfo
					{
						Name = section.Name,
						Level = section.Level,
						RelatesTo = new RelatesTo
						{
							ByteOffset = section.ByteOffset,
							LineNumber = section.LineNumber,
						},
					});
				}
			}

			if (newData.Regions != null)
			{
				foreach (Region region in newData.Regions)
				{
					snapshot.CommonData.Regions.Add(new RegionInfo
					{
						Name = region.Name,
						Minimum = region.Minimum,
						Maximum = region.Maximum,
					});
				}
			}

			snapshot.CommonData.UserRemarks = newData.SourceFileRemarks;

			return snapshot;
		}

		private static List<Filter> GetFilterHistorySnapshot(IList<string> filterHistory)
		{
			var snapshot = new List<Filter>();

			var sortOrder = 0;

			foreach (var filter in filterHistory)
			{
				sortOrder++;

				snapshot.Add(new Filter
				{
					SortOrder = sortOrder,
					Value = filter,
				});
			}

			return snapshot;
		}

		internal long GetByteOffsetOrDefault(int lineNumber)
		{
			if (lineNumber <= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(lineNumber),
					$"Value is expected to be greater than zero. Value={lineNumber}");
			}
			long offset = 0;

			SectionInfo sectionInfo = _sidecar
				.CommonData
				.TableOfContents
				?.FirstOrDefault(r => r.RelatesTo.LineNumber == lineNumber);

			if (sectionInfo != null)
			{
				offset = sectionInfo.RelatesTo.ByteOffset;
			}

			return offset;
		}
	}
}