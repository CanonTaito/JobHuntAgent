# JobHuntAgent

A job-search automation agent: it searches job listings, matches them against a candidate profile, infers hidden salary ranges, shortlists the strongest fits, and tracks applications end-to-end with human sign-off at every application.

Built as an open showcase of agent-orchestration and clean engineering (small commits, tests, CI).

## What it does, end to end

1. **It searches** — pulls fresh listings (e.g. today's ads on seek.com.au) for your roles/locations.
2. **It grades each ad** against your profile — skills, seniority, overall fit — and explains the score.
3. **It shortlists** the jobs worth applying to, ranked, with reasons.
4. **You approve** — it then drafts tailored resume bullets and a cover letter for that specific ad. Nothing is ever submitted without your OK.
5. **It tracks** — who you applied to, when to follow up, interviews, outcomes — and learns which kinds of jobs actually get replies, re-ranking future matches.

## Salary bands (a highlight feature)

Many SEEK ads say "Salary undisclosed" but still carry a hidden salary range that only powers search filters. The agent:

- uses the displayed salary when an ad has one, otherwise
- probes the search filters to find the hidden range, then
- groups jobs into bands you can browse: **Under 80k / 80-99k / 100-119k / 120-149k / 150-199k / 200k+**.

Inference runs in the background (daily/on-demand) and results are cached.

## Tech stack

| Piece | Choice |
|-------|--------|
| Backend | ASP.NET Core Minimal API (.NET 10) |
| Agent orchestration | Microsoft Agent Framework (MAF) on Microsoft.Extensions.AI |
| Local LLM | ollama — gemma3 (reasoning/JSON), nomic-embed-text (embeddings) |
| Frontend | React + TypeScript (Vite) |
| Persistence | EF Core + SQLite |
| Observability | OpenTelemetry + health checks (polish phase) |
| CI | GitHub Actions (polish phase) |

LLM access is configurable to swap local ollama for OpenAI/Azure OpenAI.

## Phased plan

Each phase is a shippable demo. Build proceeds one small commit at a time, pausing for review after every commit.

### Phase 0 — "an agent actually runs"
A demoable end-to-end slice: paste a job description → local gemma3 → structured analysis shown in a React page. Commits:
- `0.1` scaffold solution & repo conventions
- `0.2` ASP.NET Core minimal API project
- `0.3` React + Vite + TypeScript frontend
- `0.4` Ollama chat client (`Microsoft.Extensions.AI`)
- `0.5` JD Scanner agent returning structured JSON
- `0.6` `POST /api/scan` endpoint
- `0.7` Scan page in React
- `0.8` README (setup + layout)

### Phase 1 — Match scoring
Score jobs against a candidate profile using embeddings + LLM breakdown. Commits:
- `1.1` CandidateProfile model with a sample (fictional) profile in-repo; real profile stays gitignored
- `1.2` agent tools `GetProfile` / `ScoreJob`
- `1.3` embedding similarity scoring (`nomic-embed-text`)
- `1.4` structured match score in the API
- `1.5` score breakdown card in React

### Phase 2 — Pipeline, job source, salary bands
Discover → score → tailor → approve → track. Includes the salary-band engine.
- `2.1` `IJobSource` abstraction + in-memory/mock source
- `2.2` Application model with EF Core SQLite (`salaryBand`, `salarySource`)
- `2.3` discovery + scoring pipeline
- `2.4` **salary band inference engine** (displayed salary, or probe filters; align with the bands above)
- `2.5` tailoring agent (resume bullets + cover-letter draft)
- `2.6` human-in-the-loop approval API
- `2.7` tracker board + approval queue, groupable by salary band

### Phase 3 — Memory & follow-ups
- `3.1` outcome memory store
- `3.2` re-rank scoring from outcomes (learning)
- `3.3` follow-up reminder drafts

### Phase 4 — Public polish
- `4.1` SEEK search connector (live listings, rate-limited + cached)
- `4.2` salary inference on live undisclosed SEEK ads (validated against known-salary ads)
- `4.3` OpenTelemetry tracing + health checks
- `4.4` XUnit test suite (pipeline, scoring, band inference)
- `4.5` GitHub Actions build + test
- `4.6` final README with architecture diagram + demo

## How we work

- Small Conventional Commits, one concern each.
- Pause after every commit for review — you say "continue" to proceed.
- Live Seek searching is the flagship goal; the paste-a-JD screen is only the Phase 0 proof.
- The public repo ships a sample candidate profile; your real profile never enters git.