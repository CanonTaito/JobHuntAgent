# JobHuntAgent

A job-search automation agent: it searches job listings, matches them against a candidate profile, infers hidden salary ranges, shortlists the strongest fits, and tracks applications end-to-end — with human sign-off before anything is ever submitted.

**Current status: Phase 2** — scan a job description into a structured breakdown, score it against your candidate profile, and persist discovered listings to SQLite. A local LLM does the analysis.

The full roadmap lives in [PLAN.md](PLAN.md).

## Stack

| Piece | Choice |
|-------|--------|
| Backend | ASP.NET Core Minimal API (.NET 10) |
| Agent orchestration | Microsoft Agent Framework (MAF) on Microsoft.Extensions.AI |
| Chat provider | any OpenAI-compatible endpoint selected with one `AI__Provider` switch; ollama (default), gemini, groq and OpenCode Zen ship as entries under `AI:Providers`, each with retry, multi-key rotation and cross-provider fallback, via Microsoft.Extensions.AI.OpenAI |
| Frontend | React 19 + TypeScript (Vite), oxlint |
| Persistence | EF Core + SQLite |
| Observability | OpenTelemetry (traces/metrics/logs) + health checks |
| Testing | xUnit (offline fakes, in-memory SQLite) |

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
AI__Ollama__ChatModel=qwen2.5:3b AI__Ollama__SupportsTools=true dotnet run --project src/JobHunt.Api
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
| `AI:Provider` | `ollama` | key of the active entry in `AI:Providers` |
| `AI:EmbeddingProvider` | `AI:Provider` | provider used for embeddings; set this to mix a chat provider with a different embedding provider |
| `AI:FallbackProviders` | (empty) | ordered provider names to try when the active provider keeps failing |
| `AI:Providers:<name>:Endpoint` | — | OpenAI-compatible base URL |
| `AI:Providers:<name>:ChatModel` | — | chat model served by that provider |
| `AI:Providers:<name>:EmbeddingModel` | — | embedding model used for match scoring |
| `AI:Providers:<name>:ApiKeys` | — | provider secrets, normally supplied by `.env` (see below) |
| `AI:Providers:<name>:RequiresApiKey` | `true` | set to `false` for providers that need no credential (a placeholder key is sent) |
| `AI:Providers:<name>:SupportsTools` | `true` | set to `false` when the model cannot call tools; match scoring then uses a prompt-only agent |
| `AI:Retry:MaxAttempts` | `3` | total attempts before an AI call gives up |
| `AI:Retry:BaseDelayMs` | `500` | first backoff delay; doubles on each subsequent attempt |
| `AI:Retry:MaxDelayMs` | `8000` | upper bound on any backoff delay (also caps `Retry-After`) |

The shipped default is `ollama` (`gemma3` for chat, `nomic-embed-text` for embeddings). Any OpenAI-compatible provider can be added as another `AI:Providers` entry and selected with `AI__Provider=<name>`.

Chat and embedding calls retry transient failures (HTTP `429`, `5xx`, network and timeout errors) with exponential backoff, honouring a `Retry-After` header when the provider sends one. Permanent failures (`400`, `401`, `403`, and other `4xx`) fail immediately. The OpenAI SDK's own retry policy is disabled so this backoff is the single retry layer.

Match scoring normally runs as a tool-calling agent. Some chat models cannot call tools, so a provider marked `SupportsTools: false` automatically switches match scoring to a prompt-only agent that sends the redacted profile in the message and parses the JSON reply. The shipped `ollama` entry is marked this way because `gemma3` rejects tool calls; swap in a tool-capable model (see above) and flip the flag back on.

| Provider | Chat model | Embeddings | Key (`.env`) |
|----------|-----------|------------|--------------|
| `ollama` (default) | `gemma3` | `nomic-embed-text` | — (local) |
| `gemini` | `gemini-2.5-flash` | `gemini-embedding-001` | `GEMINI_API_KEY` |
| `groq` | `llama-3.3-70b-versatile` | — | `GROQ_API_KEY` |
| `zen` | `big-pickle` | `text-embedding-3-small` | `OPENCODE_ZEN_KEY` |

### Provider secrets in `.env`

API keys belong in `.env` (gitignored), never in `appsettings.json`. Each provider reads one or more keys from `<NAME>_API_KEY` (single) or `<NAME>_API_KEYS` (several), which are mapped onto `AI:Providers:<name>:ApiKeys` in order:

```bash
GEMINI_API_KEY=...          # one key
GROQ_API_KEYS=k1,k2         # comma-separated, tried in order
GEMINI_API_KEYS=["k1","k2"] # JSON array form
```

When a provider lists several keys, a call that exhausts its retries on one key advances to the next key before giving up; a `401`/`403` skips straight to the next key, while a permanent failure such as `400` stops immediately. The startup also fails fast if a provider that `RequiresApiKey` (the default) has no key configured.

`.env` keys use `:` as the configuration separator; real OS environment variables use `__`.

### Falling back across providers

`AI:FallbackProviders` is an ordered list of extra providers to try when the active one cannot serve a request:

```powershell
$env:AI__Provider = "gemini"
$env:AI__FallbackProviders__0 = "groq"
$env:AI__FallbackProviders__1 = "ollama"
```

A request works through the primary provider's keys first, then each fallback in order. It moves on after a key exhausts its retries or hits a `401`/`403`, and stops on a permanent failure such as `400`. A listed fallback with no key is skipped with a warning, so only providers you have actually configured are used. The primary provider must still be usable at startup. Embeddings follow the same chain; a fallback without an embedding model (Groq, for example) is skipped with a warning. Both texts of a match score are embedded in a single request, so a failover can never mix vectors from two different embedding models.

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

### Free-tier providers: Gemini and Groq

Both ship under `AI:Providers`; you only need the key for the provider you pick.

1. Add your key to `.env` (gitignored):

   ```bash
   GEMINI_API_KEY=...   # https://aistudio.google.com/apikey
   GROQ_API_KEY=...     # https://console.groq.com/keys
   ```

2. Select the provider for one run:

   ```powershell
   $env:AI__Provider = "gemini"; dotnet run --project src/JobHunt.Api
   ```

**Groq has no embeddings.** Match scoring needs an embedding model, so pair Groq's chat with a provider that has one:

```powershell
$env:AI__Provider = "groq"; $env:AI__EmbeddingProvider = "gemini"; dotnet run --project src/JobHunt.Api
```

The embedding chain starts at `AI:EmbeddingProvider` (or `AI:Provider` when it is unset), and that first provider must have an embedding model — fallbacks only run after it fails, so they cannot stand in for a missing model. The API fails fast at startup otherwise. Free tiers are rate limited, so large discovery runs can return `429` responses.

**Privacy:** only `ollama` keeps everything on your machine. Any cloud provider sends redacted profile text and job ads to a third party — the placeholder substitution still applies, but the request leaves your machine.

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
| SEEK | `seek` (default) | live listings from SEEK's search JSON (`seek.com.au`) |
| In-memory demo | `in-memory` | five fixed fictional ads - used for demos and tests, no network |

The SEEK connector calls SEEK's (undocumented) search endpoint, maps each ad into a `JobListing` (parsing displayed salary text, work arrangement and advertiser), and is deliberately polite: requests are **throttled** to at most one every `Jobs:Seek:MinRequestIntervalMs` and responses are **cached** in memory for `Jobs:Seek:CacheMinutes`, keyed by the query. If SEEK blocks or is unreachable the search simply returns nothing and is logged — discovery never crashes.

| Setting | Default | Notes |
|---------|---------|-------|
| `Jobs:Source` | `seek` | `seek` or `in-memory` |
| `Jobs:Seek:Endpoint` | `https://www.seek.com.au/api/chalice-search/v4/search` | the search JSON endpoint |
| `Jobs:Seek:SiteKey` | `AU-Main` | SEEK site |
| `Jobs:Seek:SourceSystem` | `houston` | SEEK's source-system parameter |
| `Jobs:Seek:PageSize` | `30` | results per page |
| `Jobs:Seek:MaxPages` | `2` | pages fetched per search |
| `Jobs:Seek:MinRequestIntervalMs` | `1000` | throttle between upstream requests |
| `Jobs:Seek:CacheMinutes` | `30` | in-memory response cache TTL |
| `Jobs:Seek:TimeoutSeconds` | `15` | per-request timeout |

```powershell
$env:Jobs__Source = "in-memory"; dotnet run --project src/JobHunt.Api
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
| `GET` | `/health/live` | liveness probe — always `Healthy` if the process is up |
| `GET` | `/health/ready` | readiness probe — `Healthy` only when the database is reachable |

## Observability

The API is instrumented with OpenTelemetry and ships health probes.

**Health checks** — `/health/live` (process up) and `/health/ready` (database reachable via an EF Core check). Wire them to a container orchestrator or load balancer; both return `200 Healthy` in plain text.

**Traces, metrics and logs** — emitted over OTLP to whatever collector you point it at (Jaeger, the Aspire dashboard, an OTel Collector). The standard OpenTelemetry environment variables configure it; with no collector running the exporters log and retry harmlessly.

| Signal | What's captured |
|--------|-----------------|
| Traces | ASP.NET Core requests (health probes excluded), outbound HTTP, EF Core queries, and agent spans — `discovery.run` → `discovery.listing` → `match.score` / `salary.infer` (with `salary.probe`), plus `tailoring.draft` and `followup.draft`. A chat or embedding call that moves to another provider records a `provider.fallback` event on the active span |
| Metrics | ASP.NET Core, HttpClient and runtime metrics, plus `jobhunt.applications.discovered` (counter) and `jobhunt.match.score` (histogram) |
| Logs | shipped over OTLP, correlated with the active trace |

```bash
# send everything to a local collector's OTLP/HTTP endpoint
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318 OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf dotnet run --project src/JobHunt.Api
```

Custom spans come from the `JobHunt.Agent` `ActivitySource` and metrics from the `JobHunt.Agent` meter — the names registered in `Program.cs`.

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
    Features/Observability/ OpenTelemetry ActivitySource + meters
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
tests/
  JobHunt.Api.Tests/      xUnit suite + fakes (job source, embeddings, in-memory SQLite, meter factory)
PLAN.md                   phased build plan
global.json               pins the .NET SDK version
```

## Development

```bash
dotnet build JobHuntAgent.slnx -warnaserror
dotnet test JobHuntAgent.slnx
npm --prefix src/JobHunt.Web run lint
npm --prefix src/JobHunt.Web run build
```

## Testing

`tests/JobHunt.Api.Tests` is an xUnit suite covering the deterministic core: salary-band classification and inference (displayed, inferred, unknown), the match-score parsing/clamping and embedding-similarity math, the discovery pipeline (scoring, persistence and de-duplication against an in-memory SQLite database), plus the SEEK salary mapper, status-transition policy, outcome re-ranker, follow-up validation and the PII redactor. The LLM and network edges are replaced with small fakes, so the suite runs offline with no model or database server.

## Roadmap

Phases 1–4 in [PLAN.md](PLAN.md): match scoring against a candidate profile, a discovery → scoring → tailoring → approval → tracking pipeline with salary-band inference, outcome memory, a live SEEK search connector, OpenTelemetry tracing + health checks, and an xUnit test suite — with CI still to come.
## Architecture

At a high level: a Minimal API (ASP.NET Core) plus a small React/Vite frontend. The API loads a candidate profile (PII-redacted in prompts), discovers jobs via IJobSource (in-memory for demo or SEEK live), scores matches (MAI agent + embeddings), infers salary bands (displayed or probed against the source), optionally tailors drafts, enforces an approval gate before marking Applied, and records outcomes for re-ranking. OpenTelemetry emits traces/metrics/logs over OTLP; health checks expose /health/live and /health/ready. The EF Core + SQLite database persists applications, outcomes and PII mappings.

## Demo

1. cp src/JobHunt.Api/profile.sample.json src/JobHunt.Api/profile.json (edit to your needs) and ollama pull gemma3:latest qwen2.5:3b nomic-embed-text:latest.
2. dotnet run --project src/JobHunt.Api (http://localhost:5051). 
pm --prefix src/JobHunt.Web run dev (http://localhost:5173).
3. Scan a JD on the Scan page, then Match on the Match page, or Discover jobs from SEEK (switch `Jobs:Source` to `in-memory` for the offline seeds). Use the Tracker to move through Scored ? Shortlisted ? Tailored ? AwaitingApproval ? Applied, record outcomes, and generate follow-ups.
