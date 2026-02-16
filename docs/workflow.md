# Workflow — GitHub Issues

This document describes how we track work items for CS2 mod projects using GitHub Issues.

## Issue Types

Every issue gets one of these labels:

| Label      | Description                                      |
|------------|--------------------------------------------------|
| `feature`  | New functionality or user-facing behavior         |
| `bug`      | Something broken that previously worked           |
| `research` | Game system investigation / decompilation work    |
| `chore`    | Build, CI, docs, dependency updates, refactoring  |

## Labels

Standard label set for every mod repo:

- **Type**: `feature`, `bug`, `research`, `chore`
- **Priority**: `priority:high`, `priority:low` (no label = normal priority)
- **Status**: `blocked`, `wontfix`

Create these labels when initializing a new repo:

```bash
gh label create feature   --color 0E8A16 --description "New functionality"
gh label create bug       --color D73A4A --description "Something broken"
gh label create research  --color 1D76DB --description "Game system investigation"
gh label create chore     --color FBCA04 --description "Build, docs, refactoring"
gh label create priority:high --color B60205 --description "Must fix before next release"
gh label create priority:low  --color C5DEF5 --description "Nice to have"
gh label create blocked   --color 000000 --description "Waiting on external dependency"
gh label create wontfix   --color FFFFFF --description "Will not be addressed"
```

## Issue Lifecycle

1. **Open** — Create issue with a type label and clear description
2. **In Progress** — Assign yourself, create a feature branch
3. **PR** — Open a pull request that references the issue
4. **Close** — Issue auto-closes when the PR merges (via commit message keywords)

## Milestones

Group issues into milestones that map to SemVer releases:

- Milestone name = version tag (e.g. `v1.0.0`, `v1.1.0`, `v2.0.0`)
- Every issue targeted for a release should be assigned to its milestone
- Close the milestone when the version is released

```bash
gh api repos/{owner}/{repo}/milestones -f title="v1.0.0" -f state="open"
```

## Branch Naming

Branches reference the issue number and follow the trunk-based branching model:

| Branch type | Pattern                         | Example                    |
|-------------|---------------------------------|----------------------------|
| Feature     | `feature/{issue}-{short-desc}`  | `feature/23-add-hotkey`    |
| Bug fix     | `fix/{issue}-{short-desc}`      | `fix/45-crash-on-load`     |
| Research    | `research/{issue}-{short-desc}` | `research/12-traffic-flow` |
| Chore       | `chore/{issue}-{short-desc}`    | `chore/8-update-deps`      |

## Commit References

Use GitHub keywords in commit messages or PR descriptions to auto-close issues on merge:

- `Fixes #123` — for bug fixes
- `Closes #123` — for features, research, chores
- `Refs #123` — to reference without closing

Example commit message:

```
Add rebindable hotkey for accident trigger

Closes #23
```

## Issue Templates

GitHub issue templates live in `.github/ISSUE_TEMPLATE/` and provide structured forms when creating issues via `gh issue create` or the web UI.

Templates included:

- **Bug Report** (`.github/ISSUE_TEMPLATE/bug_report.md`) — steps to reproduce, expected vs actual behavior, environment
- **Feature Request** (`.github/ISSUE_TEMPLATE/feature_request.md`) — description, motivation, proposed approach
