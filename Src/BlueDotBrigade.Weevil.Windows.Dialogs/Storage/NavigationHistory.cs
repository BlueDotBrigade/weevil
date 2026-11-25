namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System.Collections.Generic;

	/// <summary>
	/// Back and Forward, as in Explorer: going somewhere new clears Forward.
	/// </summary>
	internal sealed class NavigationHistory
	{
		private readonly Stack<Location> _back = new Stack<Location>();
		private readonly Stack<Location> _forward = new Stack<Location>();

		public Location Current { get; private set; }

		public bool CanGoBack => _back.Count > 0;

		public bool CanGoForward => _forward.Count > 0;

		public void Visit(Location location)
		{
			if (location.Equals(this.Current))
			{
				return;
			}

			if (this.Current is not null)
			{
				_back.Push(this.Current);
			}

			_forward.Clear();
			this.Current = location;
		}

		public Location GoBack()
		{
			_forward.Push(this.Current);
			this.Current = _back.Pop();
			return this.Current;
		}

		public Location GoForward()
		{
			_back.Push(this.Current);
			this.Current = _forward.Pop();
			return this.Current;
		}
	}
}
