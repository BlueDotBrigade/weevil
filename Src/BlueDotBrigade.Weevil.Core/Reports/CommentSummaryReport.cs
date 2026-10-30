namespace BlueDotBrigade.Weevil.Reports
{
	using System;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using Data;

	internal class CommentSummaryReport : IReport
	{
		private const string DefaultFileName = @"CommentSummary.tsv";

		private readonly RecordList _records;
		private readonly IRecordState _state;

		public CommentSummaryReport(RecordList records, IRecordState state)
		{
			_records = records;
			_state = state ?? throw new ArgumentNullException(nameof(state));
		}

		public void Generate(params object[] userParameters)
		{
			var destinationFolder = userParameters.Length == 1
				? userParameters[0].ToString()
				: throw new ArgumentException("The path to the destination folder was expected.", nameof(userParameters));

			var destinationFilePath = Path.Combine(destinationFolder, DefaultFileName);

			using (var streamWriter = new StreamWriter(destinationFilePath, false, Encoding.UTF8, 65536))
			{
				streamWriter.WriteLine("Timestamp\tComment");

				for (var i = 0; i < _records.Length; i++)
				{
					IRecord currentRecord = _records[i];

					if (_state.HasComment(currentRecord))
					{
						DateTime timestamp = DateTime.MaxValue;

						if (currentRecord.HasCreationTime)
						{
							timestamp = currentRecord.CreatedAt;
						}
						else
						{
							if (i + 1 < _records.Length)
							{
								IRecord nextRecord = _records[i + 1];
								if (nextRecord.HasCreationTime)
								{
									timestamp = nextRecord.CreatedAt;
								}
							}
						}

						if (!timestamp.Equals(DateTime.MaxValue))
						{
							var recordedAt = timestamp.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture);
							streamWriter.WriteLine($"{recordedAt}\t{_state.GetComment(currentRecord)}");
						}
					}
				}
			}
		}
	}
}
