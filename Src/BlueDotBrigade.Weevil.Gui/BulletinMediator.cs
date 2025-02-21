namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	/// <summary>
	/// Facilitates the routing of messages between view models.
	/// </summary>
	/// <seealso href="https://www.dofactory.com/net/mediator-design-pattern">DoFactory: Mediator Pattern</seealso>
	/// <seealso href="https://stackoverflow.com/a/68272972/949681">StackOverflow: MVVM communication between View Model</seealso>
	/// <credit authors="Dalstroem;MurrayVarey;" href="https://stackoverflow.com/a/68272972/949681">
	///		<license type="CC BY-SA 4.0" href="https://creativecommons.org/licenses/by-sa/4.0/" />
	///		<comments>
	///		This implementation is based on Dalstroem and MurrayVarey's sample code that was found on StackOverflow.
	///		</comments>
	/// </credit>
	[DebuggerDisplay("Subscribers={_subscriptions.Count}")]
	internal partial class BulletinMediator : IBulletinMediator
	{
		private readonly ConcurrentDictionary<BulletinKey, object> _subscriptions;

		/// <summary>
		/// Facilitates the routing of messages between view models.
		/// </summary>
		public BulletinMediator()
		{
			_subscriptions = new ConcurrentDictionary<BulletinKey, object>();
		}

		public void Subscribe<T>(object recipient, Action<T> callback)
		{
			var key = new BulletinKey(recipient, typeof(T));
			if (_subscriptions.TryAdd(key, callback))
			{
				// it worked, nothing more to do
			}
			else
			{
				throw new DuplicateSubscriptionException();
			}
		}

		public void Post<T>(T bulletin)
		{
			IEnumerable<KeyValuePair<BulletinKey, object>> result =
				from r
				in _subscriptions
				select r;

			foreach (var action in result.Select(x => x.Value).OfType<Action<T>>())
			{
				action(bulletin);
			}
		}
	}
}
