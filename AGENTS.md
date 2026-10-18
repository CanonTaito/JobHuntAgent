# JobHuntAgent — agent guide

Project plan and conventions live in `.opencode/skills/jobhunt-agent/SKILL.md`.
This file records hard rules that are easy to get wrong.

## Review & commits

- Never commit, amend, or push without the user's explicit approval. Implement the change, show `git status` and `git diff`, then pause for review.
- One concern per commit; write Conventional Commit messages (see `.opencode/skills/small-commits/SKILL.md`).

## Definition of done

Every change must pass, in order:

- `dotnet build JobHuntAgent.slnx -warnaserror`
- `dotnet test JobHuntAgent.slnx`
- `npm --prefix src/JobHunt.Web run lint`
- `npm --prefix src/JobHunt.Web run build`

CI (`.github/workflows/build.yml`) runs the same build and tests in Release.

## EF Core migrations (database schema)

The schema is defined by EF Core migrations only; `db.Database.Migrate()` at startup is the single source of truth.

- Never hand-write or hand-edit a migration. Generate it: `dotnet ef migrations add <Name> --project src/JobHunt.Api`.
- A migration must declare both `[Migration("<id>")]` and `[DbContext(typeof(JobHuntDbContext))]` (both live in the generated `.Designer.cs`). Without them EF silently ignores the migration.
- Confirm every migration is discoverable before committing: `dotnet ef migrations list --project src/JobHunt.Api`.
- Never rename, retimestamp or delete the id of a migration that any database may have recorded. A database applies migrations once per id; changing an id re-runs its `Up` (e.g. `duplicate column name`). To repair schema drift, add a *new* guarded migration.
- Never manage schema outside migrations — no raw `PRAGMA` / `ALTER` / `CREATE` at startup or in request paths.
- After adding a migration: build with `-warnaserror` (`dotnet build JobHuntAgent.slnx -warnaserror`), run the tests (`dotnet test JobHuntAgent.slnx`), then confirm the app starts against both an existing DB and a fresh one.

## Tests

- New behavior gets xUnit coverage in `tests/JobHunt.Api.Tests`.
- Replace the LLM, network and HTTP edges with small fakes; the suite must run offline with no model and no external calls (in-memory SQLite).
- Never call live providers or scrape live sites from a test.

## PII rule (non-negotiable)

- No real PII reaches any LLM, tool, embedding, log, or trace span. Substitute placeholders before a call and rehydrate afterward; the real values and mapping live in the database.

## Product guardrails

- Never auto-apply to jobs. An application only reaches `Applied` through an explicit human approval transition.
- Respect SEEK rate limits and caching; never add unthrottled scraping.

## Stack & style

- Don't change the stack: React + Vite + TypeScript SPA (never Blazor), EF Core + SQLite, Microsoft.Extensions.AI / Microsoft.Agents.AI.
- No code comments unless asked. File-scoped namespaces; `sealed` classes; records for DTOs.
- Keep feature code under `src/JobHunt.Api/Features/<Area>/`; keep API/view DTOs separate from EF entities.
- Follow `.editorconfig` (CRLF line endings; 4-space indent for C#/JSON, 2-space otherwise).
