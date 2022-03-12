namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <credit authors="Dalstroem;MurrayVarey;" href="https://stackoverflow.com/a/68272972/949681">
	///		<license type="CC BY-SA 4.0" href="https://creativecommons.org/licenses/by-sa/4.0/" />
	///		<comments>
	///		This implementation is based on Dalstroem and MurrayVarey's sample code that was found on StackOverflow.
	///		</comments>
	/// </credit>
	internal partial class BulletinMediator
	{
		protected class BulletinKey
		{
			public object Recipient { get; private set; }
			public Type BulletinType { get; private set; }

			public BulletinKey(object recipient, Type messageType)
			{
				this.Recipient = recipient;
				this.BulletinType = messageType;
			}

			protected bool Equals(BulletinKey other)
			{
				return Equals(this.Recipient, other.Recipient)
					&& Equals(this.BulletinType, other.BulletinType);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;

				return Equals((BulletinKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((this.Recipient != null ? this.Recipient.GetHashCode() : 0) * 397)
						^ ((this.BulletinType != null ? this.BulletinType.GetHashCode() : 0) * 397);
				}
			}
		}
	}
}
