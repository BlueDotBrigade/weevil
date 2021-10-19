namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;

	internal sealed class UserCommentComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.Comment.CompareTo(y.Metadata.Comment);
		}
	}
}
