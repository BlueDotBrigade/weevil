namespace BlueDotBrigade.Weevil
{
	using System;

	/// <summary>
	/// Selects a Unicode symbol that represents the workflow stage of a bookmark,
	/// based on keywords found in the bookmark label.
	/// </summary>
	/// <remarks>
	/// Priority order (highest to lowest):
	/// Bug → Verified → Fix → Root Cause → Theory / Clue → Question → Star (fallback)
	/// </remarks>
	public static class BookmarkSymbol
	{
		public const string Bug = "🪰";
		public const string Question = "❓";
		public const string Theory = "🧩";
		public const string RootCause = "🎯";
		public const string Fix = "🔧";
		public const string Verified = "✅";
		public const string Default = "⭐";

		private static readonly string[] BugKeywords =
		{
			"bug",
		};

		private static readonly string[] QuestionKeywords =
		{
			"question", "unknown", "investigate", "why", "how",
		};

		private static readonly string[] TheoryKeywords =
		{
			"theory", "hypothesis", "clue", "evidence", "observation",
		};

		private static readonly string[] RootCauseKeywords =
		{
			"root cause", "rca", "culprit", "defect", "regression",
		};

		private static readonly string[] FixKeywords =
		{
			"fix", "workaround", "mitigation", "patch", "resolution",
		};

		private static readonly string[] VerifiedKeywords =
		{
			"verified", "confirmed", "validated", "retested", "resolved",
		};

		/// <summary>
		/// Returns the symbol that best represents the workflow stage described by
		/// <paramref name="bookmarkLabel"/>. When multiple keyword groups match,
		/// the symbol with the highest configured priority is returned.
		/// </summary>
		/// <param name="bookmarkLabel">The bookmark name / label to evaluate.</param>
		/// <returns>A Unicode symbol string, or <see cref="Default"/> when no keyword matches.</returns>
		public static string GetSymbol(string bookmarkLabel)
		{
			if (string.IsNullOrEmpty(bookmarkLabel))
			{
				return Default;
			}

			var normalizedLabel = NormalizeForMatching(bookmarkLabel);

			// Evaluate in descending priority order; return as soon as a match is found.
			if (ContainsAny(normalizedLabel, BugKeywords))
			{
				return Bug;
			}

			if (ContainsAny(normalizedLabel, VerifiedKeywords))
			{
				return Verified;
			}

			if (ContainsAny(normalizedLabel, FixKeywords))
			{
				return Fix;
			}

			if (ContainsAny(normalizedLabel, RootCauseKeywords))
			{
				return RootCause;
			}

			if (ContainsAny(normalizedLabel, TheoryKeywords))
			{
				return Theory;
			}

			if (ContainsAny(normalizedLabel, QuestionKeywords))
			{
				return Question;
			}

			return Default;
		}

		private static bool ContainsAny(string normalizedLabel, string[] keywords)
		{
			foreach (var keyword in keywords)
			{
				if (normalizedLabel.IndexOf(NormalizeForMatching(keyword), StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}

		private static string NormalizeForMatching(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			return value
				.Replace(" ", string.Empty, StringComparison.Ordinal)
				.Replace("_", string.Empty, StringComparison.Ordinal)
				.Replace("-", string.Empty, StringComparison.Ordinal);
		}
	}
}
