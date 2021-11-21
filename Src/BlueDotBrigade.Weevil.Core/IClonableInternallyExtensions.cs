namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Facilitates access to the <see cref="IClonableInternally{T}"/> member methods, by avoid an explicit cast to the interface type.
	/// </summary>
	/// <seealso href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation">MSDN: Explicit Interface Implementation</seealso>
	/// <seealso href="https://alexfranchuk.com/blog/internal-interface-classes-in-csharp/">Alex Franchuk: Internal Interface Classes in C#</seealso>
	/// <seealso href="https://stackoverflow.com/a/1253277/949681">Stackoverflow: Why Explicit Implementation of a Interface can not be public?</seealso>
	internal static class IClonableInternallyExtensions
	{
		public static T GetDeepCopy<T>(this IClonableInternally<T> instance)
		{
			return instance.CreateDeepCopy();
		}
	}
}
