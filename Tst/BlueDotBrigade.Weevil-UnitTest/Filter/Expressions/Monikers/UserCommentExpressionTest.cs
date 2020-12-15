namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class UserCommentExpressionTest
	{
		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsTrue()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Metadata).Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment");

			Assert.IsTrue(filter.IsMatch(record.Object));
		}

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsFalse()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Metadata).Returns(new Metadata());

			var filter = new UserCommentExpression("@Comment");

			Assert.IsFalse(filter.IsMatch(record.Object));
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsTrue()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Metadata).Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=dog");

			Assert.IsTrue(filter.IsMatch(record.Object));
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsFalse()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Metadata).Returns(new Metadata { Comment = "The lazy brown dog jumped over the fence" });

			var filter = new UserCommentExpression("@Comment=cat");

			Assert.IsFalse(filter.IsMatch(record.Object));
		}
	}
}
