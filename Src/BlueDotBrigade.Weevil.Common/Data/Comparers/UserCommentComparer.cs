namespace BlueDotBrigade.Weevil.Data.Comparers
{
	using System.Collections.Generic;

	internal sealed class UserCommentComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.Comment.CompareTo(y.Metadata.Comment);
		}
	}
}
