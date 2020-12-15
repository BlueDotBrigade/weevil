namespace BlueDotBrigade.Weevil
{
	internal interface IClonableInternally<T>
	{
		T CreateDeepCopy();
	}
}
