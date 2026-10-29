namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using Data;

	public interface IExpression
	{
		/// <param name="record">The record to test.</param>
		/// <param name="content">Holds the record's content. An expression that tests the text reads it from here, so a record's content is read once for every expression (#1131).</param>
		bool IsMatch(IRecord record, ContentBuffer content);
	}
}
