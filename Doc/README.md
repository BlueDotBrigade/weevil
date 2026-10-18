# Weevil Documentation

Supporting documentation for developers. Pick the folder by the question you are asking.

| Folder | Answers | Contents | Kept up to date? |
|---|---|---|---|
| [Design](Design/) | How does Weevil work today? | Architecture, component notes, requirements | Yes: update it in the same pull request as the code |
| [Guides](Guides/) | How should I do X? | Naming, UI style guide, WPF, testing, requirements syntax | Yes, when the practice changes |
| [Research](Research/) | Why was it built this way? | Concepts, benchmarks and prototypes behind a decision | No: a dated record of what was known then |
| [Plans](Plans/) | What is the plan for issue #N? | Implementation plans written by people and AI agents | Only the status note |
| [LessonsLearned](LessonsLearned/) | What surprised us? | Short notes on behaviour that was not obvious, one per file | Rarely |
| [Notes/Release](Notes/Release/) | What ships with Weevil? | Help, release notes, licences | Every release |

`Notes/Release` must not move: the installer and the in-app release notification depend on its paths.

## One Source of Truth

When a tool can check a rule, the tool's configuration is the source of truth, and documents only explain why. For example, code style lives in [`.editorconfig`](../.editorconfig), not in a guide.

AI agent instructions live in [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) and [`.github/instructions/`](../.github/instructions/), shared by GitHub Copilot and Claude Code. See [AiAgentInstructionFiles.md](LessonsLearned/AiAgentInstructionFiles.md).

## Research

Start with the question and the issue number. End with the decision and its status, so a reader knows whether the idea was adopted.

## Plans

Name a plan after its issue: `<IssueNumber>-<ShortName>.md`, for example `978-RecordColumns.md`.

Every plan starts with a status note, directly under its title:

```markdown
> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.
```

| Status | Meaning |
|---|---|
| Draft | Being written; not approved |
| Approved | Approved; work has not started |
| In progress | Being implemented |
| Done | Implemented. Describe the result in `Design/`; leave the plan as a record |
| Abandoned | Will not be implemented |

## Lessons Learned

One short note per lesson, named after the symptom or topic so a search finds it, for example `GitHubIssueTypesVsLabels.md`.
