namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class UserCommentExpressionTest
	{
		private const int LineNumber = 5;

		private readonly AnnotationStore _annotations = new AnnotationStore();
		private readonly IRecord _record = R.WithLineNumber(LineNumber);

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsTrue()
		{
			_annotations.SetComment(LineNumber, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment", _annotations);

			(filter.IsMatch(_record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsFalse()
		{
			var filter = new UserCommentExpression("@Comment", _annotations);

			(filter.IsMatch(_record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsTrue()
		{
			_annotations.SetComment(LineNumber, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment=dog", _annotations);

			(filter.IsMatch(_record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsFalse()
		{
			_annotations.SetComment(LineNumber, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment=cat", _annotations);

			(filter.IsMatch(_record)).Should().BeFalse();
		}
	}
}
