Plan for Using GitHub Copilot Instruction Files Effectively

Objective

Create a structured set of instruction and context files in the repository to improve Copilot performance for:
	•	Feature implementation
	•	Bug fixing
	•	Pull request review
	•	Test generation

This plan focuses only on architecture and organization of instruction files, not their detailed implementation.

⸻

1. Core Principle

The most important file is:

.github/copilot-instructions.md

This file provides repository-wide guidance and is used across:
	•	GitHub.com Copilot Chat
	•	GitHub.com coding agent
	•	GitHub.com code review
	•	Visual Studio Copilot Chat
	•	Visual Studio code review

Because of this broad coverage, it should contain only rules that apply universally across the repository.

Key contents:
	•	High level repository overview
	•	Architecture summary
	•	Directory structure map
	•	Standard build commands
	•	Standard test commands
	•	Validation steps before PR completion
	•	Global coding conventions
	•	Error handling expectations
	•	Logging conventions
	•	Security rules
	•	Files or folders Copilot should not modify

Important constraint:
Code review instructions only read the first ~4000 characters, so the most critical rules must appear at the top.

⸻

2. Path-Specific Instruction Files

Use the folder:

.github/instructions/

These files apply only to certain paths using applyTo glob patterns.

They allow different rules for different parts of the codebase.

Recommended initial files:
	•	backend.instructions.md
	•	frontend.instructions.md
	•	tests.instructions.md
	•	docs.instructions.md
	•	infra.instructions.md

Each file should document conventions specific to that domain.

Typical sections:

Purpose
Naming conventions
Code style
Error handling
Security considerations
Testing expectations
Performance considerations

Example responsibilities:

Backend
	•	Service architecture
	•	Validation patterns
	•	Persistence rules
	•	Logging strategy
	•	Async/background job conventions

Frontend
	•	Component patterns
	•	State management rules
	•	Accessibility requirements
	•	API client conventions

Tests
	•	Testing framework usage
	•	Fixture strategy
	•	Mocking rules
	•	Naming conventions

Docs
	•	README structure
	•	Architecture documentation
	•	Changelog expectations

Infrastructure
	•	GitHub Actions conventions
	•	Secret management
	•	Deployment safeguards

⸻

3. Review-Specific Instructions

Create a file such as:

.github/instructions/review.instructions.md

With configuration:

applyTo: “**”
excludeAgent: “coding-agent”

This file focuses specifically on pull request review quality.

Topics to include:
	•	Missing tests
	•	Security risks
	•	Performance regressions
	•	Backwards compatibility
	•	Unsafe migrations
	•	Logging and observability

⸻

4. Coding Agent Execution Guidance

Create a file such as:

.github/instructions/build-validation.instructions.md

Configuration:

applyTo: “**”
excludeAgent: “code-review”

This file contains guidance that helps the coding agent operate safely.

Examples:
	•	Command order for build/test
	•	Dependency installation steps
	•	Environment configuration notes
	•	Generated files that should not be edited
	•	Validation steps before proposing code

⸻

5. AGENTS.md (Later Phase)

AGENTS.md files are useful for agent-specific behavior and can appear anywhere in the repository.

The nearest AGENTS.md file takes precedence.

Use these when specialized AI behaviors are required for certain directories.

Examples:
	•	Testing specialist agent
	•	Documentation generator
	•	Security review agent

However this should come after the base instruction system is stable.

⸻

6. Prompt Files for IDE Workflows

Prompt files support reusable workflows primarily inside IDE environments.

Location:

.github/prompts/

Useful prompts:
	•	implement-feature.prompt.md
	•	fix-bug.prompt.md
	•	write-tests.prompt.md
	•	review-changes.prompt.md
	•	draft-pr-summary.prompt.md

These files provide structured prompts but do not replace repository instruction files.

⸻

7. Copilot Spaces

Spaces should hold task-specific context rather than permanent repository rules.

Typical contents:
	•	Feature specifications
	•	Design documents
	•	Pull requests
	•	Issues
	•	Notes
	•	Meeting transcripts
	•	Mockups

Spaces allow Copilot to reason about a specific task with richer context.

⸻

8. Copilot Memory

Copilot Memory stores repository-specific insights learned during usage.

Characteristics:
	•	Repository scoped
	•	Automatically validated against the codebase
	•	Automatically expires

Memory complements instruction files but should not replace them.

Stable rules belong in repository files.

⸻

9. Coding Agent Setup File

Consider adding:

copilot-setup-steps.yml

This file helps the coding agent reliably initialize the project environment.

Typical contents:
	•	dependency installation
	•	environment setup
	•	build initialization

Without this file, the agent may attempt to infer setup steps incorrectly.

⸻

10. Anti-Patterns to Avoid

Do not create extremely large instruction files.

Do not place critical rules after the first 4000 characters of the main file.

Do not rely on external links for critical instructions.

Do not put language-specific rules in the global instruction file.

Do not try to control Copilot comment formatting using instruction files.

Do not rely on personal Copilot instructions as a repository policy.

⸻

11. Recommended Implementation Order
	1.	Create .github/copilot-instructions.md
	2.	Add path-specific instruction files under .github/instructions/
	3.	Add review-specific instruction file
	4.	Add coding-agent validation file
	5.	Enable automatic Copilot PR reviews
	6.	Add prompt files for IDE workflows
	7.	Introduce AGENTS.md files if specialized behavior is required
	8.	Use Spaces for feature-level context
	9.	Add copilot-setup-steps.yml for coding agent reliability

⸻

Summary

The strategy is to build a layered context system for GitHub CoPilot that will help to produce better results when using GitHub.com tools & Visual Studio 2026 (plus CoPilot extension).

Global repository rules
→ Path-specific rules
→ Review-specific guidance
→ Coding agent execution rules
→ Prompt files for workflows
→ Spaces for task context
→ Memory for learned repository insights

This layered approach ensures Copilot agents produce more reliable implementations, better tests, and higher quality pull request reviews.