---
name: gherkin-authoring
description: Use when the user asks to write, draft, generate, author, review, critique, lint, or improve Gherkin scenarios, .feature files, Cucumber, SpecFlow, Reqnroll, or Behave specifications, or when a Jira story / acceptance criterion needs BDD scenarios. Covers both generation from requirements and review of existing scenarios for anti-patterns.
---

# Gherkin Authoring

Authoritative house style for Gherkin scenarios. Wraps Andrew Knight's *Gherkin Guidelines for AI* (AutomationPanda, MIT, pinned at commit `ce2b49e`) as the substantive ruleset.

## When to use

- User provides a user story, acceptance criterion, or requirement and asks for scenarios → **Generation mode**.
- User pastes a `.feature` file or scenario and asks for review, critique, lint, or improvement → **Review mode**.

## Procedure

1. **Load the ruleset.** Read `references/gherkin-guidelines.md` in full before writing or reviewing. It is the source of truth; do not paraphrase from memory.
2. **Apply the house overrides below** wherever they tighten the referenced guidelines.
3. **Generation mode:**
   - If the business rule is unclear, ask one targeted clarifying question before writing.
   - Produce scenarios in declarative, domain-language style. Target ≤ 8 steps per scenario.
   - Use `Scenario Outline` only for true equivalence classes; otherwise prefer separate `Scenario` blocks.
   - Emit `Background` only if ≥ 2 scenarios share the same precondition.
   - After the scenarios, output a short "Design notes" block explaining any non-obvious choices (why these scenarios, why not an outline, what was deliberately left out).
4. **Review mode:**
   - For each scenario, walk the "Common anti-patterns" section of `references/gherkin-guidelines.md` and check the quick checklist at the bottom of that file.
   - For every issue: cite line number, name the rule violated, and propose a concrete rewrite.
   - End with a short "What this feature does well" section.

## House overrides (apply on top of the referenced ruleset)

These tighten the referenced guidelines where this project's convention is stricter:

- **One `When` per scenario.** The referenced guide allows `And`-continued actions; this project does not. Multiple `When`s indicate two scenarios.
- **Target 3–8 steps**, not just "< 10".
- **Background is rare.** Default to no `Background`; introduce one only when ≥ 2 scenarios in the same file genuinely share the precondition.
- **Scenario titles state the rule, not the test.** "Customer receives free shipping over $50" — not "Test shipping" or "Verify shipping works".

## Constraints

- Never invent business rules not implied by the input.
- Never include UI selectors, CSS/XPath, sleeps, technical IDs, or tool verbs ("click", "navigate", "wait") in `.feature` files.
- Never chain scenarios — no scenario depends on state left by another.
- If a project glossary or `.ears` requirement is provided, prefer those exact terms (ubiquitous language).

## Files

- `references/gherkin-guidelines.md` — substantive ruleset (Andrew Knight / AutomationPanda, MIT).
- `LICENSE`, `README.md`, `CHANGELOG.md` — upstream metadata, pinned at commit `ce2b49e786197dc18575a5760fab46d573dc5ee5`.