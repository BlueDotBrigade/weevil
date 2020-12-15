namespace BlueDotBrigade.Weevil.IO
{
	using System.Collections.Immutable;
	using Data;

	public interface IWrite
	{
		void Write(ImmutableArray<IRecord> records);
	}
}
