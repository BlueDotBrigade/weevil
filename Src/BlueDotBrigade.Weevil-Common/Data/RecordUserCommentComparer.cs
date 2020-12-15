namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	public sealed class RecordUserCommentComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.Comment.CompareTo(y.Metadata.Comment);
		}
	}
}
