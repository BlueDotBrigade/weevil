namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Collections.Generic;

	[DebuggerDisplay("Include={this.Include}, Exclude={this.Exclude}, Configuration={this.Configuration.Count}")]
	public class FilterCriteria : IFilterCriteria, IEquatable<FilterCriteria>
	{
		/// <summary>
		/// Indicates that all records should be returned.
		/// </summary>
		public static readonly FilterCriteria None = new FilterCriteria(
			string.Empty,
			string.Empty,
			new ReadOnlyDictionary<string, object>(new Dictionary<string, object>()));


		/// <summary>
		/// Records that match the <paramref name="include"/> filter will be returned.
		/// </summary>
		public FilterCriteria(string include)
			 : this(include, string.Empty, None.Configuration)
		{
			// nothing to do
		}

		/// <summary>
		/// Records that match the <paramref name="include"/> filter, and do not match the <paramref name="exclude"/> will be returned.
		/// </summary>
		/// <remarks>
		/// In the event that both the <paramref name="include"/> and <paramref name="exclude"/> parameters are <see cref="string.Empty">, all records will be returned.</see>
		/// </remarks>
		public FilterCriteria(string include, string exclude)
			 : this(include, exclude, None.Configuration)
		{
			// nothing to do
		}

		public FilterCriteria(string include, string exclude, Dictionary<string, object> configuration)
			 : this(include, exclude, new ReadOnlyDictionary<string, object>(configuration))
		{
			// nothing to do
		}

		public FilterCriteria(string include, string exclude, IReadOnlyDictionary<string, object> configuration)
		{
			this.Include = string.IsNullOrEmpty(include) ? string.Empty : include;
			this.Exclude = string.IsNullOrEmpty(exclude) ? string.Empty : exclude;
			this.Configuration = configuration ?? None.Configuration;
		}

		public string Include { get; private set; }

		public string Exclude { get; private set; }

		public IReadOnlyDictionary<string, object> Configuration { get; private set; }

		public bool Equals(FilterCriteria other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (this.Include == other.Include && this.Exclude == other.Exclude && this.Configuration.Count == other.Configuration.Count)
			{
				var sameMetadata = true;
				foreach (var key in this.Configuration.Keys)
				{
					if (other.Configuration.ContainsKey(key))
					{
						sameMetadata = this.Configuration[key] == other.Configuration[key];
						if (!sameMetadata)
						{
							break;
						}
					}
				}

				return sameMetadata;
			}
			else
			{
				return false;
			}
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((FilterCriteria)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (this.Include != null ? this.Include.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (this.Exclude != null ? this.Exclude.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (this.Configuration != null ? this.Configuration.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(FilterCriteria left, FilterCriteria right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(FilterCriteria left, FilterCriteria right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			var include = (string.IsNullOrWhiteSpace(this.Include)) ? "(not specified)" : this.Include;
			var exclude = (string.IsNullOrWhiteSpace(this.Exclude)) ? "(not specified)" : this.Exclude;

			var configuration = this.Configuration.ToString("=", ";");
			configuration = (string.IsNullOrWhiteSpace(configuration)) ? "(none)" : this.Exclude;

			return $"{nameof(this.Include)}: {include}, {nameof(this.Exclude)}: {exclude}, {nameof(this.Configuration)}: {configuration}";
		}
	}
}
