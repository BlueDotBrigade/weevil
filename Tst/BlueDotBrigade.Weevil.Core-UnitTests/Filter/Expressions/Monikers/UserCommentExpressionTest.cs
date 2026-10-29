namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class UserCommentExpressionTest
	{
		private const int LineNumber = 5;

		private readonly RecordState _state = new RecordState();
		private readonly IRecord _record = R.WithLineNumber(LineNumber);

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsTrue()
		{
			_state.SetComment(_record, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment", _state);

			(filter.IsMatch(_record, new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForAnyComment_ReturnsFalse()
		{
			var filter = new UserCommentExpression("@Comment", _state);

			(filter.IsMatch(_record, new ContentBuffer())).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsTrue()
		{
			_state.SetComment(_record, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment=dog", _state);

			(filter.IsMatch(_record, new ContentBuffer())).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_LookingForSpecificComment_ReturnsFalse()
		{
			_state.SetComment(_record, "The lazy brown dog jumped over the fence");

			var filter = new UserCommentExpression("@Comment=cat", _state);

			(filter.IsMatch(_record, new ContentBuffer())).Should().BeFalse();
		}
	}
}
