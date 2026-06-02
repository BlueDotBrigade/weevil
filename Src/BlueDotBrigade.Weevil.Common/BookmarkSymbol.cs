namespace BlueDotBrigade.Weevil
{
	using System;

	/// <summary>
	/// Selects a Unicode symbol that represents the workflow stage of a bookmark,
	/// based on keywords found in the bookmark label.
	/// </summary>
	/// <remarks>
	/// Priority order (highest to lowest):
	/// Verified → Fix → Root Cause → Theory / Clue → Question → Star (fallback)
	/// </remarks>
	public static class BookmarkSymbol
	{
		public const string Question = "❓";
		public const string Theory = "🧩";
		public const string RootCause = "🎯";
		public const string Fix = "🔧";
		public const string Verified = "✅";
		public const string Default = "⭐";

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
		/// <paramref name="bookmarkLabel"/>.  When multiple keyword groups match,
		/// the symbol for the furthest stage in the investigation workflow is returned.
		/// </summary>
		/// <param name="bookmarkLabel">The bookmark name / label to evaluate.</param>
		/// <returns>A Unicode symbol string, or <see cref="Default"/> when no keyword matches.</returns>
		public static string GetSymbol(string bookmarkLabel)
		{
			if (string.IsNullOrEmpty(bookmarkLabel))
			{
				return Default;
			}

			// Evaluate in descending priority order; return as soon as a match is found.
			if (ContainsAny(bookmarkLabel, VerifiedKeywords))
			{
				return Verified;
			}

			if (ContainsAny(bookmarkLabel, FixKeywords))
			{
				return Fix;
			}

			if (ContainsAny(bookmarkLabel, RootCauseKeywords))
			{
				return RootCause;
			}

			if (ContainsAny(bookmarkLabel, TheoryKeywords))
			{
				return Theory;
			}

			if (ContainsAny(bookmarkLabel, QuestionKeywords))
			{
				return Question;
			}

			return Default;
		}

		private static bool ContainsAny(string label, string[] keywords)
		{
			foreach (var keyword in keywords)
			{
				if (label.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}
