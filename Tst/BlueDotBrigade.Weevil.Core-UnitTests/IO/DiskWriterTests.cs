namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Text;
	using System.Threading;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;
	using NSubstitute;

	[TestClass]
	public class DiskWriterTests
	{
		private const string EarlierText = "Saved earlier";

		private string _folder = string.Empty;
		private string _destinationPath = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = System.IO.Directory.CreateTempSubdirectory("Weevil.DiskWriterTests.").FullName;
			_destinationPath = Path.Combine(_folder, "Saved.tsv");
			System.IO.File.WriteAllText(_destinationPath, EarlierText);
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenAnEarlierFile_WhenASaveIsAbortedPartWay_ThenTheEarlierFileIsKeptAndNoPartialFileIsLeft()
		{
			// Abort stops a save within one record, and the destination changes only once every record is written (#1027).
			using var abort = new CancellationTokenSource();
			IRecordState state = Substitute.For<IRecordState>();
			var recordsWritten = 0;
			state.IsFlagged(Arg.Any<IRecord>()).Returns(_ =>
			{
				if (++recordsWritten == 2)
				{
					abort.Cancel();
				}

				return false;
			});

			Action save = () => CreateWriter(state).Write(CreateRecords(), abort.Token);

			save.Should().Throw<OperationCanceledException>();
			System.IO.File.ReadAllText(_destinationPath).Should().Be(EarlierText);
			System.IO.Directory.GetFiles(_folder).Should().ContainSingle("the records written before the abort are deleted");
		}

		[TestMethod]
		public void GivenAnEarlierFile_WhenSaved_ThenItIsReplaced()
		{
			CreateWriter(new RecordState()).Write(CreateRecords(), CancellationToken.None);

			System.IO.File.ReadAllText(_destinationPath).Should().Contain("Third");
			System.IO.Directory.GetFiles(_folder).Should().ContainSingle();
		}

		private DiskWriter CreateWriter(IRecordState state)
		{
			return new DiskWriter(_destinationPath, Encoding.UTF8, FileFormatType.Tsv, state);
		}

		private static RecordList CreateRecords()
		{
			return R.Create()
				.WithContent("First")
				.WithContent("Second")
				.WithContent("Third")
				.GetRecords();
		}
	}
}
