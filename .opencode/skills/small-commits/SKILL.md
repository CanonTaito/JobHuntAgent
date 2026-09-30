---
name: small-commits
description: Use when making code changes in any project that uses the small-commit workflow. Enforces decomposing work into small commits (one concern each), Conventional Commit messages, and pausing for user review after each commit. Trigger keywords: commit plan, small commits, conventional commits, review cadence, one commit at a time.
---

# Small, reviewable commits

This project works in small commits so the user can review incremental changes without being overwhelmed.

## Rules

1. **Decompose first.** Break any task into a sequence of commits, each touching one concern.
2. **One concern per commit.** A commit should compile and represent a single visible increment. Do not bundle unrelated changes.
3. **Conventional Commit messages.** Use the prefix types:
   - `feat:` new functionality
   - `fix:` bug fixes
   - `chore:` scaffolding, tooling, housekeeping
   - `docs:` documentation only
   - `test:` tests only
   - `ci:` CI / workflow changes
   - `refactor:` behavior-preserving restructuring
   - `perf:` performance
   Message body optional; keep the summary imperative and short ("add", "wire", "return", not "added").
4. **Verify before commit.** Build / run the relevant checks so the commit lands green.
5. **Pause after every commit.** After committing, stop and let the user review. Only continue to the next commit on their explicit "continue".
6. **Never silence-multi-commit.** Unless the user explicitly asks to batch, do not run several commits without pausing.

## Workflow loop

1. Present the next commit plan.
2. Implement.
3. Build to verify.
4. Commit with a Conventional Commit message.
5. Pause for review.
6. Repeat on "continue".