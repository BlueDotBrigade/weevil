# GitHub: Issue Types, Labels and Sub-Issues

Issue types and labels look alike, but they do different jobs:

- An **issue type** says what an issue *is*. An issue has one type, and types are shared by every repository in the organization.
- A **label** is a tag. An issue can have many labels, and each repository has its own.

GitHub does not define epics, user stories or requirements. Those are Agile terms, and they map onto issue types and sub-issues.

> Notes from #983, 2026-10-18. The Weevil setup at the end is a proposal, not yet adopted.

## What GitHub Documents

| Feature | Documented behaviour |
|---|---|
| Issue type | Defined by the organization, up to 25. The defaults are Task, Bug and Feature, and can be changed. Shown in issue lists; searchable with `type:"Bug"`; usable in Project views. Issue forms can set it with `type:`, and GitHub Actions can run when it changes (`typed`, `untyped`). ([types][Types], [forms][Forms], [events][Events]) |
| Label | Defined per repository; applies to issues, pull requests and discussions. "Useful for keeping track of project goals, bugs, types of work, and the status of an issue." The `good first issue` label fills the repository's *contribute* page. ([labels][Labels], [planning][Planning]) |
| Sub-issue | Breaks larger work into smaller issues: up to 100 per parent, 8 levels deep, across repositories. Projects can group by "Parent issue" and show "Sub-issue progress". ([sub-issues][SubIssues], [fields][SubIssueFields]) |
| Issue dependency | Marks an issue as "blocked by" or "blocking" another. ([dependencies][Dependencies]) |
| Issue field | Structured metadata defined by the organization, "instead of relying on labels". The defaults are Priority, Effort, Start date and Target date. Each field is pinned to issue types, so the type decides which fields appear. ([issue fields][IssueFields]) |
| Milestone | Due date, percent complete, and open and closed counts. ([milestones][Milestones]) |

GitHub never says that issue types replace labels.

## Does the Issue Type Change What Copilot Does?

Not according to GitHub's documentation. When an issue is assigned to Copilot, it "gets sent the issue title, description, any comments that currently exist, and any additional instructions you provide" ([Copilot][CopilotAssign]). The type is not in that list. GitHub's advice is to treat the issue as a prompt: describe the problem, the acceptance criteria, and the files to change ([tips][CopilotTips]).

An agent that reads issues through the API can see the type, but it acts on the type only if its instructions say so.

## Agile Terms (Not GitHub's)

| Term | Meaning | In GitHub |
|---|---|---|
| Epic | A large goal made of several stories, delivered over several iterations | Parent issue |
| User story | A small slice of user value, with acceptance criteria | Issue of type Feature |
| Feature | A product capability. SAFe uses it as a level between epic and story; Weevil does not need that level. | Same as a user story |
| Requirement | A testable "shall" statement, written with EARS | Not a work item: it describes the product, so it belongs in `Doc/Design/` |

## GitHub's Best Practices for Projects

From [Best practices for Projects][ProjectBestPractices]:

1. Communicate across issues and pull requests: mentions, assignees, links.
2. Break large issues into smaller issues. This "leads to smaller pull requests, which are easier to review."
3. Use the project description, README and status updates.
4. Create views: table, board, roadmap.
5. Use custom fields, such as a number field for complexity.
6. Automate: for example, set the status to Done when an issue closes.
7. Use charts to show progress.
8. Use project templates to share a setup.
9. Keep a single source of truth: each fact in one place.

## Proposed Setup for Weevil

Not adopted yet. On 2026-10-18 Weevil had:

- Labels `user_story`, `bug` and `bug_report`. The last two mean the same thing: the bug template adds `bug`, while triaged bugs use `bug_report`.
- An `epic` label used by no issue.
- 58 open issues labelled `requirement`, one per "shall" statement.
- `Size_*` labels for estimates.
- No open milestones and no Project.
- Sub-issues already in use: #964 and #682.

| Piece | Proposal |
|---|---|
| Issue types | Epic, Feature (user story), Bug, Task |
| Hierarchy | Epic → Feature, Bug or Task. A third level only when one story needs several pull requests. |
| Labels to keep | Area labels (`ANALYSIS`, `FILTER`, `NAVIGATION`, `COMMAND_LINE`, `CONFIGURATION`, `REPORTING`, `METRICS`), plus `performance`, `technical_debt`, `documentation`, `known_issue`, `help_wanted` and `good first issue` |
| Labels to replace | `user_story` → Feature type; `bug` and `bug_report` → Bug type; `epic` → Epic type; `Size_*` → Effort field; `blocked` → issue dependency; `will_not_fix` → close as "not planned" |
| Requirements | EARS statements in `Doc/Design/`, linked from each story's acceptance criteria |
| Milestones | One per release |
| Project | One project: a board by status, a table grouped by parent issue, a roadmap by milestone |
| Issue templates | Issue forms that set the issue type |

Once adopted, the rules belong in `CONTRIBUTING.md`. GitHub links that file from every new issue and pull request, but only finds it in the repository root, `docs` or `.github` folder ([contributing][Contributing]).

[Types]: https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/managing-issue-types-in-an-organization
[Forms]: https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms
[Events]: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#issues
[Labels]: https://docs.github.com/en/issues/using-labels-and-milestones-to-track-work/managing-labels
[Planning]: https://docs.github.com/en/issues/tracking-your-work-with-issues/learning-about-issues/planning-and-tracking-work-for-your-team-or-project
[SubIssues]: https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/adding-sub-issues
[SubIssueFields]: https://docs.github.com/en/issues/planning-and-tracking-with-projects/understanding-fields/about-parent-issue-and-sub-issue-progress-fields
[Dependencies]: https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/creating-issue-dependencies
[IssueFields]: https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/managing-issue-fields-in-your-organization
[Milestones]: https://docs.github.com/en/issues/using-labels-and-milestones-to-track-work/about-milestones
[CopilotAssign]: https://docs.github.com/en/copilot/how-tos/use-copilot-agents/cloud-agent/use-cloud-agent-on-github
[CopilotTips]: https://docs.github.com/en/copilot/tutorials/cloud-agent/get-the-best-results
[ProjectBestPractices]: https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/best-practices-for-projects
[Contributing]: https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/setting-guidelines-for-repository-contributors
