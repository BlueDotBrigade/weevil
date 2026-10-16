namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using Diagnostics;
	using Log = Diagnostics.Log;

	internal class MultilineRecordParser
	{
		/// <summary>
		/// Represents the number of lines of a file that will be read while searching for the beginning of a new record.
		/// </summary>
		public const int MaximumLinesToSearch = 256;

		public const int FirstRecordLineNumber = 1;

		public const bool LoggingEnabled = true;

		private readonly StreamReader _dataSource;
		private readonly IRecordParser _recordParser;
		private readonly int _maximumLinesToSearch;
		private readonly bool _isLoggingEnabled;
		private int _lineNumber;

		private IRecord _currentRecord;
		private IRecord _nextRecord;

		public MultilineRecordParser(StreamReader dataSource, IRecordParser recordParser)
			: this(dataSource, recordParser, MaximumLinesToSearch, FirstRecordLineNumber, LoggingEnabled)
		{
			// nothing to do
		}

		public MultilineRecordParser(StreamReader dataSource, IRecordParser recordParser, int maximumLinesToSearch, int firstRecordLineNumber, bool isLoggingEnabled)
		{
			_maximumLinesToSearch = maximumLinesToSearch > 0 ? maximumLinesToSearch : throw new ArgumentOutOfRangeException(
				 nameof(maximumLinesToSearch),
				 maximumLinesToSearch,
				 "Value is expected to be greater than zero.");

			_lineNumber = firstRecordLineNumber - 1;
			_dataSource = dataSource;
			_recordParser = recordParser;
			_maximumLinesToSearch = maximumLinesToSearch;
			_isLoggingEnabled = isLoggingEnabled;
			_currentRecord = Record.Dummy;
			_nextRecord = Record.Dummy;
		}

		/// <summary>
		///     Returns true if the data stream has more content.
		/// </summary>
		private bool IsDataAvailable => !_dataSource.EndOfStream;

		public IRecord GetNext()
		{
			if (BeginReadNext())
			{
				EndReadNext();
			}
			else
			{
				if (this.IsDataAvailable)
				{
					var streamPosition = -1L;
					if (!_dataSource.EndOfStream)
					{
						if (_dataSource.BaseStream.CanRead)
						{
							streamPosition = _dataSource.BaseStream.Position;
						}
					}

					if (_isLoggingEnabled)
					{
						Log.Default.Write(
							LogSeverityType.Warning, string.Format(CultureInfo.InvariantCulture,
								$"Unable to find the beginning of a record. CurrentStreamPosition={streamPosition}"));
					}
				}
				else
				{
					if (_isLoggingEnabled)
					{
						Log.Default.Write(
							LogSeverityType.Debug,
							"The end of file (EOF) has been reached.");
					}
				}
			}

			return _currentRecord;
		}

		private bool BeginReadNext()
		{
			var wasRecordFound = false;

			_currentRecord = Record.Dummy;

			var linesRead = 0;

			if (Record.IsGenuine(_nextRecord))
			{
				_currentRecord = _nextRecord;
				_nextRecord = Record.Dummy;
				wasRecordFound = true;
			}
			else
			{
				while (Record.IsDummyOrNull(_currentRecord) && this.IsDataAvailable && linesRead < _maximumLinesToSearch)
				{
					var line = _dataSource.ReadLine();

					linesRead++;
					_lineNumber++;

					// Get beginning of current record
					if (_recordParser.TryParse(_lineNumber, line, out _currentRecord))
					{
						wasRecordFound = true;
					}
					else
					{
						if (_isLoggingEnabled)
						{
							Log.Default.Write(
								LogSeverityType.Error, string.Format(CultureInfo.InvariantCulture,
									"The record appears to be corrupt, or incomplete. LineNumber={0}", _lineNumber));
						}
					}
				}
			}

			return wasRecordFound;
		}

		private void EndReadNext()
		{
			var isLoading = true;

			// TODO: How much does this improve performance by turning variable into a member field?
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(_currentRecord.Content);

			var isMultilineRecord = false;

			do
			{
				if (this.IsDataAvailable)
				{
					var temp = _dataSource.ReadLine();
					_lineNumber++;

					// Have we found the next record?
					if (_recordParser.TryParse(_lineNumber, temp, out _nextRecord))
					{
						isLoading = false;

						_currentRecord = new Record(
							 _currentRecord.LineNumber,
							 _currentRecord.CreatedAt,
							 _currentRecord.Severity,
							 stringBuilder.ToString(),
							 _currentRecord.Metadata);

						_currentRecord.Metadata.IsMultiLine = isMultilineRecord;
					}
					else
					{
						isMultilineRecord = true;
						stringBuilder.Append("\r\n");
						stringBuilder.Append(temp);
					}
				}
				else
				{
					isLoading = false;

					_currentRecord = new Record(
						 _currentRecord.LineNumber,
						 _currentRecord.CreatedAt,
						 _currentRecord.Severity,
						 stringBuilder.ToString(),
						 _currentRecord.Metadata);

					_currentRecord.Metadata.IsMultiLine = isMultilineRecord;
				}
			} while (isLoading);
		}
	}
}