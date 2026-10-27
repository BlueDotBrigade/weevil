# Which Instruction Files Each AI Agent Reads

No single instruction file is read by Claude Code, Codex and every GitHub Copilot feature:

- `.github/copilot-instructions.md` is the only file every Copilot feature reads. `AGENTS.md` looks like the shared standard, but Copilot Chat on github.com, Copilot in Visual Studio, and Copilot code review in VS Code and Visual Studio ignore it.
- Claude Code reads `CLAUDE.md`. It reads `AGENTS.md` only when there is no `CLAUDE.md`, and it never reads `.github/copilot-instructions.md` or `.github/instructions/` on its own.
- Codex reads `AGENTS.md` and none of the other files on its own. The Codex agent joins every `AGENTS.md` from the repository root down to its working folder. Its code review on GitHub uses the one nearest each changed file, and follows that file's `## Review guidelines` section.

> Notes from #983, 2026-10-18. Codex added from #1111, 2026-10-27.

## How Weevil Keeps One Source

The instructions live in Copilot's files. Claude Code imports them with `@path` lines, and `AGENTS.md` names them for Codex:

| File | Read by | Contains |
|---|---|---|
| `.github/copilot-instructions.md` | Every Copilot feature; Claude Code through `CLAUDE.md` | The rules for the whole repository |
| `.github/instructions/*.instructions.md` | Copilot, for files matching `applyTo`; Claude Code through the `CLAUDE.md` in that folder | The rules for one folder |
| `CLAUDE.md` | Claude Code | Only imports: the repository rules and the review rules |
| `AGENTS.md` | Codex; also Copilot's cloud agent, its code review on github.com and Chat in VS Code | Only references: the repository rules, the folder rules and, under `## Review guidelines`, the review rules |
| `Src/BlueDotBrigade.Weevil.Common/CLAUDE.md`, `Src/BlueDotBrigade.Weevil.Core/CLAUDE.md`, `Src/BlueDotBrigade.Weevil.Gui/CLAUDE.md`, `Tst/CLAUDE.md` | Claude Code, once it reads or edits a file in that folder | Only an import of that folder's instructions file |

To add rules for another folder, create `.github/instructions/<name>.instructions.md` with an `applyTo` pattern, and a `CLAUDE.md` in that folder containing `@<relative path>/.github/instructions/<name>.instructions.md`.

Claude Code strips HTML comments (`<!-- -->`) from `CLAUDE.md`, so the `CLAUDE.md` files explain themselves in a comment without adding to Claude's context. Copilot features that read `CLAUDE.md` see only the comment and the import lines.

Claude Code also saves the plans it writes in plan mode to `Doc/Plans/`, set by `plansDirectory` in `.claude/settings.json`.

## Which Copilot Feature Reads What

From [Support for different types of custom instructions][CopilotSupport]:

| Copilot feature | `.github/copilot-instructions.md` | `.github/instructions/` | `AGENTS.md` / `CLAUDE.md` |
|---|---|---|---|
| Chat on github.com | Yes | No | No |
| Cloud agent (all environments) | Yes | Yes | Yes |
| Code review on github.com | Yes | Yes | Yes |
| Chat in VS Code | Yes | Yes | `AGENTS.md` only |
| Code review in VS Code | Yes | No | No |
| Chat in Visual Studio | Yes | Yes | No |
| Code review in Visual Studio | Yes | No | No |

Claude Code: [How Claude remembers your project][ClaudeMemory] (`CLAUDE.md`, `@path` imports, `AGENTS.md`) and [`plansDirectory`][ClaudePlans].

Codex: [Codex code review in GitHub][CodexReview] (`AGENTS.md` and its review guidelines). Codex has no `@path` imports, so `AGENTS.md` names the files to read in plain sentences.

[CopilotSupport]: https://docs.github.com/en/copilot/reference/custom-instructions-support
[ClaudeMemory]: https://code.claude.com/docs/en/memory
[ClaudePlans]: https://code.claude.com/docs/en/settings-reference#plansdirectory
[CodexReview]: https://developers.openai.com/codex/integrations/github
