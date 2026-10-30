namespace BlueDotBrigade.Weevil.IO
{
	using System.Collections.Immutable;
	using Data;

	public interface IWrite
	{
		void Write(RecordList records);
	}
}
