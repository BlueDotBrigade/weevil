﻿namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class UserCommentExpressionTest
	{
		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment");

			Assert.IsTrue(filter.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata());

			var filter = new UserCommentExpression("@Comment");

			Assert.IsFalse(filter.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=dog");

			Assert.IsTrue(filter.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=cat");

			Assert.IsFalse(filter.IsMatch(record));
		}
	}
}
