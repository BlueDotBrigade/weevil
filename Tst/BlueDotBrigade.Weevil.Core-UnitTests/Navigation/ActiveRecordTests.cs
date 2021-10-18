﻿namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ActiveRecordTests
	{
		[TestMethod]
		[ExpectedException((typeof(ArgumentException)))]
		public void Constructor_UninitializedArray_ThrowsArgumentException()
		{
			var activeRecord = new ActiveRecord(new ImmutableArray<IRecord>());

			Assert.Fail("Uninitialized immutable array should throw exception.");
		}

		[TestMethod]
		public void GoToNext_FirstSearch_ReturnsRecord10()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var navigator = new ActiveRecord(records);
			navigator.GoToNext(record => record.LineNumber > 0);

			Assert.AreEqual(
				10,
				navigator.Record.LineNumber);
		}

		[TestMethod]
		public void GoToNext_SecondSearch_ReturnsRecord20()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var navigator = new ActiveRecord(records);
			navigator.GoToNext(record => record.LineNumber > 0);
			navigator.GoToNext(record => record.LineNumber > 0);

			Assert.AreEqual(
				20,
				navigator.Record.LineNumber);
		}

		[TestMethod]
		public void GoToPrevious_FirstSearch_ReturnsRecord30()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var navigator = new ActiveRecord(records);
			navigator.GoToPrevious(record => record.LineNumber > 0);

			Assert.AreEqual(
				30,
				navigator.Record.LineNumber);
		}

		[TestMethod]
		public void GoToPrevious_SecondSearch_ReturnsRecord30()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var navigator = new ActiveRecord(records);
			navigator.GoToPrevious(record => record.LineNumber > 0);
			navigator.GoToPrevious(record => record.LineNumber > 0);

			Assert.AreEqual(
				20,
				navigator.Record.LineNumber);
		}

		[TestMethod]
		[ExpectedException((typeof(ArgumentException)))]
		public void UpdateDataSource_UninitializedArray_ThrowsArgumentException()
		{
			var activeRecord = new ActiveRecord(new ImmutableArray<IRecord>());

			Assert.Fail("Uninitialized immutable array should throw exception.");
		}

		[TestMethod]
		public void UpdateDataSource_ActiveRecordNoLongerExists_DefaultActiveRecord()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var navigator = new ActiveRecord(records);
			navigator.SetActiveLineNumber(10);

			navigator.UpdateDataSource(ImmutableArray.Create<IRecord>());

			Assert.AreEqual(
				Record.Dummy,
				navigator.Record);
		}
	}
}