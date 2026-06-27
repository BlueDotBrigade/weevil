namespace BlueDotBrigade.Weevil.Common.UnitTests
{
	[TestClass]
	public class BookmarkSymbolTests
	{
		[TestMethod]
		public void GetSymbol_NullLabel_ReturnsDefault()
		{
			BookmarkSymbol.GetSymbol(null).Should().Be(BookmarkSymbol.Default);
		}

		[TestMethod]
		public void GetSymbol_EmptyLabel_ReturnsDefault()
		{
			BookmarkSymbol.GetSymbol(string.Empty).Should().Be(BookmarkSymbol.Default);
		}

		[TestMethod]
		public void GetSymbol_NoKeyword_ReturnsDefault()
		{
			BookmarkSymbol.GetSymbol("Interesting sequence of events").Should().Be(BookmarkSymbol.Default);
		}

		[DataTestMethod]
		[DataRow("bug found in parser")]
		[DataRow("Bug investigation")]
		public void GetSymbol_BugKeyword_ReturnsBug(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Bug);
		}

		// ── Question keywords ────────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("Question: why did login fail?")]
		[DataRow("Unknown error observed")]
		[DataRow("Investigate the retry loop")]
		[DataRow("Why does this happen?")]
		[DataRow("How did the cache expire?")]
		public void GetSymbol_QuestionKeyword_ReturnsQuestion(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Question);
		}

		// ── Theory keywords ──────────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("Theory: connection pool exhausted")]
		[DataRow("Hypothesis: stale cache entry")]
		[DataRow("Clue found in logs")]
		[DataRow("Evidence of retry storm")]
		[DataRow("Observation: high CPU spike")]
		public void GetSymbol_TheoryKeyword_ReturnsTheory(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Theory);
		}

		// ── Root Cause keywords ───────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("RCA: stale cache entry")]
		[DataRow("Root cause identified")]
		[DataRow("Culprit: null pointer dereference")]
		[DataRow("Defect in serialization")]
		[DataRow("Regression introduced in v2.3")]
		public void GetSymbol_RootCauseKeyword_ReturnsRootCause(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.RootCause);
		}

		[DataTestMethod]
		[DataRow("root-cause identified")]
		[DataRow("root_cause identified")]
		public void GetSymbol_RootCauseKeywordsWithSpacesUnderscoresOrHyphens_AreMatched(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.RootCause);
		}

		[DataTestMethod]
		[DataRow("work-around for customer")]
		[DataRow("work_around for customer")]
		public void GetSymbol_FixKeywordsWithSpacesUnderscoresOrHyphens_AreMatched(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Fix);
		}

		// ── Fix keywords ──────────────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("Fix deployed")]
		[DataRow("Workaround for customer")]
		[DataRow("Mitigation applied")]
		[DataRow("Patch deployed to production")]
		[DataRow("Resolution merged")]
		public void GetSymbol_FixKeyword_ReturnsFix(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Fix);
		}

		// ── Verified keywords ─────────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("Verified after retest")]
		[DataRow("Confirmed fix works")]
		[DataRow("Validated in staging")]
		[DataRow("Retested and passing")]
		[DataRow("Resolved by engineering")]
		public void GetSymbol_VerifiedKeyword_ReturnsVerified(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Verified);
		}

		// ── Case insensitivity ────────────────────────────────────────────────

		[DataTestMethod]
		[DataRow("VERIFIED fix applied")]
		[DataRow("Verified RCA after patch deployment")]
		public void GetSymbol_VerifiedKeywordUpperCase_ReturnsVerified(string label)
		{
			BookmarkSymbol.GetSymbol(label).Should().Be(BookmarkSymbol.Verified);
		}

		// ── Priority: Verified wins over everything else ──────────────────────

		[TestMethod]
		public void GetSymbol_VerifiedAndFixKeywords_ReturnsVerified()
		{
			// "Verified" (highest priority) wins over "fix"
			BookmarkSymbol.GetSymbol("Verified fix applied").Should().Be(BookmarkSymbol.Verified);
		}

		[TestMethod]
		public void GetSymbol_VerifiedAndRootCauseKeywords_ReturnsVerified()
		{
			BookmarkSymbol.GetSymbol("Verified RCA after patch deployment").Should().Be(BookmarkSymbol.Verified);
		}

		[TestMethod]
		public void GetSymbol_FixAndRootCauseKeywords_ReturnsFix()
		{
			// "Fix" has higher priority than "Root Cause"
			BookmarkSymbol.GetSymbol("Fix for the defect").Should().Be(BookmarkSymbol.Fix);
		}

		[TestMethod]
		public void GetSymbol_RootCauseAndTheoryKeywords_ReturnsRootCause()
		{
			// "Root Cause" has higher priority than "Theory"
			BookmarkSymbol.GetSymbol("RCA based on evidence").Should().Be(BookmarkSymbol.RootCause);
		}

		[TestMethod]
		public void GetSymbol_TheoryAndQuestionKeywords_ReturnsTheory()
		{
			// "Theory" has higher priority than "Question"
			BookmarkSymbol.GetSymbol("Theory: why did this fail?").Should().Be(BookmarkSymbol.Theory);
		}

		[TestMethod]
		public void GetSymbol_BugAndVerifiedKeywords_ReturnsBug()
		{
			BookmarkSymbol.GetSymbol("Verified bug after retest").Should().Be(BookmarkSymbol.Bug);
		}
	}
}
