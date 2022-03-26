namespace BlueDotBrigade.Weevil.Gui
{
	using System;

	internal interface IBulletinMediator
	{
		void Subscribe<T>(object recipient, Action<T> callback);
		void Post<T>(T message);
	}
}