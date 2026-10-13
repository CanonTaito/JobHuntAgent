# JobHuntAgent

A job-search automation agent: it searches job listings, matches them against a candidate profile, infers hidden salary ranges, shortlists the strongest fits, and tracks applications end-to-end — with human sign-off before anything is ever submitted.

**Current status: Phase 2** — scan a job description into a structured breakdown, score it against your candidate profile, and persist discovered listings to SQLite. A local LLM does the analysis.

The full roadmap lives in [PLAN.md](PLAN.md).

## Stack

| Piece | Choice |
|-------|--------|
| Backend | ASP.NET Core Minimal API (.NET 10) |
| Agent orchestration | Microsoft Agent Framework (MAF) on Microsoft.Extensions.AI |
| Chat provider | ollama (default) or OpenCode Zen — one config switch; both speak the OpenAI API via Microsoft.Extensions.AI.OpenAI |
| Frontend | React 19 + TypeScript (Vite), oxlint |
| Persistence | EF Core + SQLite |

## Prerequisites

- [.NET SDK 10.0.401](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- Node.js 20.19+ or 22.12+ (verified on 22.16.0)
- [Ollama](https://ollama.com) running locally, with the chat model pulled:

  ```bash
  ollama pull gemma3
  ```

## Quick start

```bash
# API — http://localhost:5051
dotnet run --project src/JobHunt.Api --urls http://localhost:5051

# Web — http://localhost:5173 (proxies /api to the API above)
npm --prefix src/JobHunt.Web install
npm --prefix src/JobHunt.Web run dev
```

Open <http://localhost:5173>. The **Tracker** opens first — hit **Search jobs** to discover and score listings from your profile, then group them by salary band. For a one-off, switch to **Scan a job description** or **Score a match** and paste a description. On a CPU-only machine with `gemma3`, expect roughly a minute per scan.

Scanning and tailoring work with any chat model. **Match scoring and discovery** call agent tools, so they need a tool-capable model — e.g. `qwen2.5:3b`:

```bash
AI__Ollama__ChatModel=qwen2.5:3b dotnet run --project src/JobHunt.Api
```

Try the API directly:

```bash
curl -X POST http://localhost:5051/api/scan \
  -H "Content-Type: text/plain" \
  --data-binary @job-description.txt
```

Discover and score listings from the job source (query is derived from your profile preferences), inferring a salary band for each:

```bash
curl -X POST "http://localhost:5051/api/discover?max=5"
```

Draft tailored resume bullets and a cover letter for a stored application (uses the application id returned by discover):

```bash
curl -X POST "http://localhost:5051/api/applications/1/tailor"
```

Nothing is ever applied automatically. Move a tailored application into the approval queue, then approve (or reject) it:

```bash
curl -X POST "http://localhost:5051/api/applications/1/request-approval"
curl -X POST "http://localhost:5051/api/applications/1/approve"
```

## Configuration

All AI settings live under the `AI` section of `src/JobHunt.Api/appsettings.json` and can be overridden with environment variables using .NET's `__` separator (`AI__Provider=zen`). Job-source settings live under `Jobs` (see [Job sources](#job-sources)).

| Setting | Default | Notes |
|---------|---------|-------|
| `AI:Provider` | `ollama` | `ollama` or `zen` |
| `AI:Ollama:Endpoint` | `http://localhost:11434/v1` | ollama's OpenAI-compatible endpoint |
| `AI:Ollama:ChatModel` | `gemma3` | any chat model ollama has pulled |
| `AI:Ollama:EmbeddingModel` | `nomic-embed-text` | used from Phase 1 for match scoring |
| `AI:Zen:Endpoint` | `https://opencode.ai/zen/v1` | |
| `AI:Zen:ChatModel` | `big-pickle` | |
| `AI:Zen:ApiKey` | — | from `OPENCODE_ZEN_KEY` in `.env` |

### Using OpenCode Zen instead of ollama

1. Copy `.env.example` to `.env` (gitignored) and set your key:

   ```bash
   OPENCODE_ZEN_KEY=sk-...
   ```

2. Switch the provider for one run:

   ```bash
   AI__Provider=zen dotnet run --project src/JobHunt.Api
   ```

   On PowerShell:

   ```powershell
   $env:AI__Provider = "zen"; dotnet run --project src/JobHunt.Api
   ```

   Caveat: Zen's free tier only answers calls made from inside opencode. External apps need a funded Console account, so ollama stays the default.

The API also walks up from `src/JobHunt.Api` looking for a `.env`, so the key is picked up without extra setup.

## Candidate profile

The agent scores jobs against a candidate profile (Phase 1+). A fictional sample ships in the repo:

- **Default:** `src/JobHunt.Api/profile.sample.json` is loaded when nothing else is found.
- **Yours:** copy it to `src/JobHunt.Api/profile.json` and edit it — `profile*.json` is gitignored except the sample, so your details never enter git.
- **Anywhere:** or set `Profile:Path` (env: `Profile__Path`) to any absolute or content-root-relative path.

Inspect what loaded: `GET http://localhost:5051/api/profile`. Only `name` is required — every other field is optional and profession-agnostic.

## Privacy

No personally identifiable information ever reaches the chat or embedding model. Starting with Phase 1, every prompt is built from placeholder-substituted text (e.g. `[NAME]`, `[EMPLOYER_1]`): the real values and the placeholder mapping are kept only locally in SQLite (see [Database](#database)) and re-substituted when responses come back. The known PII values — name (plus its first/last tokens), employers, institutions, and the profile's location — are replaced wherever they appear in profile text, including free-text fields. See exactly what the model would receive via `GET /api/profile/redacted`. With the default ollama provider, everything stays on your machine. Job ads are public content and pass through unchanged.

## Database

Phase 2 adds SQLite persistence via EF Core. The database file lives next to the API project at `src/JobHunt.Api/jobhunt.db` (gitignored — it holds the PII placeholder mapping) and migrations apply automatically on startup. Point it elsewhere with `ConnectionStrings:JobHunt` (env: `ConnectionStrings__JobHunt`); a relative `Data Source` is resolved against the API's content root.

Two tables:

| Table | Holds |
|-------|-------|
| `Applications` | discovered/scored listings, including `SalaryBand`, `SalarySource` (`Displayed`/`Inferred`), `Status`, the match scores (`OverallScore`, `SkillsScore`, …), the tailored draft (`TailoredBullets`, `CoverLetter`, `TailoredAt`), and the follow-up draft (`FollowUpSubject`, `FollowUpBody`, `FollowUpSendAfterDays`, `FollowUpDraftedAt`) |
| `Outcomes` | the append-only outcome memory — one row per recorded result (`Outcome`, `Note`, `RecordedAt`) linked to its application |
| `PiiMappings` | the placeholder ↔ real-value map from the privacy rule above — real values never leave this table, only placeholders reach the model |

After changing the model, add a migration:

```bash
dotnet ef migrations add <Name> --project src/JobHunt.Api --startup-project src/JobHunt.Api --output-dir Features/Persistence/Migrations
```

## Job sources

Discovery runs through the `IJobSource` abstraction, so the agent searches a pluggable source. Pick one with `Jobs:Source` (env: `Jobs__Source`):

| Source | Value | Notes |
|--------|-------|-------|
| In-memory demo | `in-memory` (default) | five fixed fictional ads — used for demos and tests, no network |
| SEEK | `seek` | live listings from SEEK's search JSON (`seek.com.au`) |

The SEEK connector calls SEEK's (undocumented) search endpoint, maps each ad into a `JobListing` (parsing displayed salary text, work arrangement and advertiser), and is deliberately polite: requests are **throttled** to at most one every `Jobs:Seek:MinRequestIntervalMs` and responses are **cached** in memory for `Jobs:Seek:CacheMinutes`, keyed by the query. If SEEK blocks or is unreachable the search simply returns nothing and is logged — discovery never crashes.

| Setting | Default | Notes |
|---------|---------|-------|
| `Jobs:Source` | `in-memory` | `in-memory` or `seek` |
| `Jobs:Seek:Endpoint` | `https://www.seek.com.au/api/chalice-search/v4/search` | the search JSON endpoint |
| `Jobs:Seek:SiteKey` | `AU-Main` | SEEK site |
| `Jobs:Seek:SourceSystem` | `houston` | SEEK's source-system parameter |
| `Jobs:Seek:PageSize` | `30` | results per page |
| `Jobs:Seek:MaxPages` | `2` | pages fetched per search |
| `Jobs:Seek:MinRequestIntervalMs` | `1000` | throttle between upstream requests |
| `Jobs:Seek:CacheMinutes` | `30` | in-memory response cache TTL |
| `Jobs:Seek:TimeoutSeconds` | `15` | per-request timeout |

```powershell
$env:Jobs__Source = "seek"; dotnet run --project src/JobHunt.Api
```

SEEK exposes no public API and uses anti-bot protection, so keep the throttle and cache on and expect occasional empty results.

## Salary bands

Many ads say "Salary undisclosed" yet still carry a hidden range used only to power search filters. The engine labels every listing with a band and records how it was found (`SalarySource`):

- **`Displayed`** — the ad states a salary; the band comes straight from it.
- **`Inferred`** — no salary shown, so the engine probes the source's salary filter bucket by bucket; the ad silently drops out once the filter crosses its hidden range, revealing the band it sits in.
- **`Unknown`** — nothing could be recovered.

Bands: **Under 80k · 80-99k · 100-119k · 120-149k · 150-199k · 200k+**. Run `POST /api/discover` and each new application is persisted with its inferred band; the tracker groups applications by it.

The same engine runs against live SEEK ads: a probe becomes a `salaryrange=<min>-<max>` filter, and undisclosed ads come back labelled. Because the technique is a guess, it is **validated against ads that do state a salary** — their displayed band must equal the band the probe infers. `GET /api/salary/validate` samples the current source, re-infers each known-salary ad by probing (ignoring the displayed figure), and reports the agreement:

```bash
curl "http://localhost:5051/api/salary/validate?sample=10"
```

```json
{ "sampled": 4, "knownSalary": 2, "validated": 2, "accurate": 2, "accuracy": 1.0,
  "checks": [ { "sourceId": "…", "displayedBand": "80-99k", "inferredBand": "80-99k", "matched": true } ],
  "inferred": [ { "sourceId": "…", "band": "120-149k", "source": "Inferred" } ] }
```

## Approval gate

The agent never applies to anything. Drafting only moves an application to `Tailored`; the agent cannot reach `Applied` on its own — that transition only happens when a human approves it. The lifecycle is:

```
Discovered → Scored → Shortlisted → Tailored → AwaitingApproval → Applied → Archived
```

Allowed transitions are declared in `ApplicationStatusTransitions`; any move that is not listed (e.g. jumping straight from `Scored` to `Applied`, or re-approving an already-`Applied` job) is rejected with `409 Conflict`.

## Outcome memory

Once an application is `Applied`, the agent remembers what happened to it. Outcomes are an append-only log, so a single application can progress — `NoResponse` → `Interview` → `Offer` — each step recorded with a timestamp and an optional note:

```
POST /api/applications/1/outcome   { "outcome": "Interview", "note": "Recruiter call booked." }
GET  /api/applications/1/outcomes
```

Outcomes can only be recorded for applications that were actually submitted (`Applied` or `Archived`) — recording one on a job that is merely `Scored` returns `409 Conflict`. Outcomes are stored locally and never sent to a model.

## Learning (outcome re-ranking)

Outcome memory feeds back into scoring. The read model builds reply-rate insights from the outcomes, and a `MatchReRanker` nudges each application's stored `OverallScore` toward the salary bands and work models that have actually produced replies (an `Interview` or `Offer` counts as a reply). The result is returned as `RankScore`, with a plain-English `RankReason`, and the list is ordered by it — so the stored score stays the model's raw judgement while the ranking reflects what has been working.

It stays quiet until there is enough evidence: at least `MatchReRanker.MinimumApplications` applications with outcomes, and at least `MinimumBucketSamples` in a bucket before that bucket can move the score. Adjustments are capped at ±`MaxAdjustment` (15) so a small sample can't dominate.

Inspect what the agent has learned:

```bash
curl http://localhost:5051/api/applications/ranking-insights
```

This returns the reply rate per band and per work model. See the effect on the board via `GET /api/applications`, which now carries `rankScore` (the re-ranked score) alongside `overallScore` (the raw one).

## Follow-ups

For an application you have actually submitted (`Applied`), the agent can draft a follow-up reminder — a short, polite nudge to send if the employer goes quiet:

```bash
curl -X POST http://localhost:5051/api/applications/1/follow-up
```

It returns a `subject`, a `body`, and a `sendAfterDays` hint (clamped to 3–21 days), and persists all three on the application (`FollowUpSubject`, `FollowUpBody`, `FollowUpSendAfterDays`, `FollowUpDraftedAt`) so the board can surface it. Like tailoring, the draft is generated from the redacted profile plus the public job ad, then rehydrated before it is stored or shown — the model never sees your real details. Drafting is refused with `409 Conflict` unless the application is `Applied`.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/` | liveness |
| `GET` | `/api/ai/ping` | round-trips a trivial prompt through the configured chat model |
| `GET` | `/api/profile` | the loaded candidate profile (contains your PII — for local verification) |
| `GET` | `/api/profile/redacted` | the same profile with PII replaced by placeholders — what the LLM sees |
| `POST` | `/api/scan` | scans a raw job description (request body = text) and returns the structured analysis |
| `POST` | `/api/match` | scores a raw job description against the profile (request body = text) — overall, embedding similarity, and the LLM breakdown |
| `POST` | `/api/discover` | discovers listings from the job source, scores each new one, infers its salary band, and persists it; `?max=N` caps how many (default 5) |
| `GET` | `/api/salary/validate` | samples the source and validates the salary-probe inference against ads that state a salary; `?sample=N` caps how many (default 10) |
| `POST` | `/api/applications/{id}/tailor` | drafts tailored resume bullets and a cover letter for a stored application and persists them (status → `Tailored`); `404` if the id is unknown |
| `GET` | `/api/applications` | lists applications best-first by learned `RankScore`; filter with `?status=Scored`, etc. — `400` for an unknown status |
| `GET` | `/api/applications/ranking-insights` | the reply rate per salary band and work model that drives re-ranking |
| `GET` | `/api/applications/{id}` | reads one application; `status` and `salarySource` are strings, not enum numbers |
| `POST` | `/api/applications/{id}/request-approval` | moves `Tailored → AwaitingApproval` (into the approval queue); `409` from any other state |
| `POST` | `/api/applications/{id}/approve` | the human gate — `AwaitingApproval → Applied`; `409` from any other state |
| `POST` | `/api/applications/{id}/reject` | moves an active application to `Rejected`; `409` once `Applied`/`Archived` |
| `POST` | `/api/applications/{id}/outcome` | records an outcome (`NoResponse`/`Interview`/`Offer`/`Rejected`/`Withdrawn`) for a submitted application; `409` if it has not been `Applied` |
| `POST` | `/api/applications/{id}/follow-up` | drafts (and persists) a follow-up reminder — subject, body and a `sendAfterDays` hint — for an `Applied` application; `409` if it has not been `Applied` |
| `GET` | `/api/applications/{id}/outcomes` | lists an application's recorded outcomes, newest first |

## Layout

```
src/
  JobHunt.Api/            ASP.NET Core Minimal API
    Ai/                   config-switchable chat + embedding wiring, .env loader
    Features/Applications/ the JobApplication entity, status enum, read model + queries
    Features/Approvals/   approval gate: status-transition policy + service
    Features/Discovery/   discovery → score → persist pipeline
    Features/FollowUps/   follow-up reminder agent (subject, body, sendAfterDays) + service
    Features/JdScan/      the JD Scanner agent + result model + JSON parser
    Features/Jobs/        IJobSource connector seam + in-memory source
    Features/Jobs/Seek/   live SEEK search connector (throttled + cached) + mapper
    Features/Match/       GetProfile + ScoreJob agent tools for match scoring
    Features/Outcomes/    outcome memory store (EF-backed application outcomes)
    Features/Persistence/ EF Core SQLite context, migrations, registration
    Features/Pii/         PII placeholder engine (redact out, rehydrate back)
    Features/Profile/     candidate profile model + startup loader
    Features/Ranking/     outcome-memory insights + the re-ranker
    Features/Salary/      salary-band model, displayed/inferred inference engine + validator
    Features/Tailoring/   tailoring agent (resume bullets + cover letter) and service
    Program.cs            endpoints + DI composition root
    profile.sample.json   fictional sample profile (profile.json is yours, gitignored)
  JobHunt.Web/            React + Vite frontend
    src/scan.ts           typed client for POST /api/scan
    src/ScanPage.tsx      the Scan page
    src/match.ts          typed client for POST /api/match
    src/MatchPage.tsx     the Match page with the score breakdown card
    src/tracker.ts        typed client for the application + approval endpoints
    src/TrackerPage.tsx   the tracker board (group by band/status, approval queue)
PLAN.md                   phased build plan
global.json               pins the .NET SDK version
```

## Development

```bash
dotnet build JobHuntAgent.slnx -warnaserror
npm --prefix src/JobHunt.Web run lint
npm --prefix src/JobHunt.Web run build
```

## Roadmap

Phases 1–4 in [PLAN.md](PLAN.md): match scoring against a candidate profile, a discovery → scoring → tailoring → approval → tracking pipeline with salary-band inference, outcome memory, and a live SEEK search connector — with OpenTelemetry and a test suite still to come.