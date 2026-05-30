namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
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

			(filter.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata());

			var filter = new UserCommentExpression("@Comment");

			(filter.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=dog");

			(filter.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Metadata.Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=cat");

			(filter.IsMatch(record)).Should().BeFalse();
		}
	}
}
