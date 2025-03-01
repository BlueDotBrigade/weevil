namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public struct StorageUnit
	{
		public const ulong BytesPerKilobyte = 1024;
		public const ulong BytesPerMegabyte = 1024 * BytesPerKilobyte;
		public const ulong BytesPerGigabyte = 1024 * BytesPerMegabyte;

		public static readonly StorageUnit Zero = new StorageUnit(0);

		#region Fields
		private readonly ulong _bytes;
		#endregion

		#region Object Lifetime

		public StorageUnit(int bytes = 0)
		{
			if (bytes < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bytes));
			}

			_bytes = (ulong)bytes;
		}

		public StorageUnit(uint bytes)
		{
			_bytes = (ulong)bytes;
		}

		public StorageUnit(long bytes)
		{
			if (bytes < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bytes));
			}

			_bytes = (ulong) bytes;
		}

		public StorageUnit(ulong bytes)
		{
			_bytes = bytes;
		}

		#endregion

		#region Properties

		public ulong Bytes { get { return _bytes; } }

		public double KiloBytes => (_bytes / (double)BytesPerKilobyte);

		public double MetaBytes => (_bytes / (double)BytesPerMegabyte);

		public double GigaBytes => (_bytes / (double)BytesPerGigabyte);

		#endregion

		#region Operators
		public static StorageUnit operator -(StorageUnit left, StorageUnit right)
		{
			return left.Subtract(right);
		}

		public static StorageUnit operator +(StorageUnit value)
		{
			return value;
		}

		public static StorageUnit operator +(StorageUnit left, StorageUnit right)
		{
			return left.Add(right);
		}

		public override bool Equals(object that)
		{
			if (that == null)
			{
				return false;
			}
			else
			{
				if (that is StorageUnit thatInstance)
				{
					return this == thatInstance;
				}
				else
				{
					return false;
				}
			}
		}

		public bool Equals(StorageUnit that)
		{
			return _bytes == that._bytes;
		}

		public override int GetHashCode()
		{
			return _bytes.GetHashCode();
		}

		public static bool operator ==(StorageUnit left, StorageUnit right)
		{
			return left.Bytes == right.Bytes;
		}

		public static bool operator !=(StorageUnit left, StorageUnit right)
		{
			return left.Bytes != right.Bytes;
		}

		public static bool operator <(StorageUnit left, StorageUnit right)
		{
			return left.Bytes < right.Bytes;
		}

		public static bool operator <=(StorageUnit left, StorageUnit right)
		{
			return left.Bytes <= right.Bytes;
		}

		public static bool operator >(StorageUnit left, StorageUnit right)
		{
			return left.Bytes > right.Bytes;
		}

		public static bool operator >=(StorageUnit left, StorageUnit right)
		{
			return left.Bytes >= right.Bytes;
		}
		#endregion

		public StorageUnit Add(StorageUnit value)
		{
			var result = checked(this.Bytes + value.Bytes);
			return new StorageUnit(result);
		}

		public StorageUnit Subtract(StorageUnit value)
		{
			var result = checked(this.Bytes - value.Bytes);
			return new StorageUnit(result);
		}
	}
}
