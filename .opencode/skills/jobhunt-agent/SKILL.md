---
name: jobhunt-agent
description: Use when building, extending, or reviewing the JobHuntAgent project (job search automation agent, SEEK job connector, salary band inference, JD scanner). Enforces the phased, small-commit build plan, stack decisions, and human-in-the-loop review workflow. Trigger keywords: JobHuntAgent, job search agent, JD, salary band, salary inference, SEEK, seek.com.au, application tracker, match scoring.
---

# JobHuntAgent

A job-search automation agent that searches job listings, scores matches against a candidate profile, infers hidden salary bands, tracks applications, and learns from outcomes. Built for the public GitHub repo `CanonTaito/JobHuntAgent` as a showcase of agent-orchestration and clean-engineering habits.

## Fixed stack decisions

- Backend: ASP.NET Core Minimal API on **.NET 10**.
- Agent orchestration: **Microsoft Agent Framework (MAF)** — GA since April 2026, built on `Microsoft.Extensions.AI`. Use `chatClient.AsAIAgent()` + `FunctionInvokingChatClient` for tools; MAF workflow patterns for orchestration.
- Chat provider via **`Microsoft.Extensions.AI.OpenAI` (GA v10)** — the MEAI Ollama package is not GA, so both providers are wired as OpenAI-compatible endpoints behind config: **ollama** (default, zero cost, `http://localhost:11434/v1`; `gemma3` for reasoning / structured JSON, `qwen2.5:3b` for tool calls when gemma3 lacks tools, `nomic-embed-text` for embeddings) or **OpenCode Zen** (`https://opencode.ai/zen/v1`, model `big-pickle`, API key `OPENCODE_ZEN_KEY`). Switching providers is config-only.
  - Zen caveat: the free tier gate (403 `FreeTierError`) only allows calls made from *inside* opencode; external apps need a funded Console account or a local opencode server. Hence ollama is the default.
- Frontend: **React + TypeScript (Vite) SPA** — never Blazor for the UI.
- Persistence: **EF Core + SQLite**.
- OTel tracing + health checks (polish phase).

## The user's vision (non-negotiable product behavior)

1. The agent **searches** (e.g. fresh listings on seek.com.au) — the user must NOT have to paste a job description. The paste-a-JD screen is only the Phase 0 demo.
2. Each ad is **scored** against the user's resume/profile and a ranked shortlist is returned with reasons.
3. **Salary bands**: every ad has either a displayed salary or an inferred one (see below). Users group jobs by band (e.g. "show the 100–119k jobs").
4. Human approves before anything is applied. Never auto-apply.
5. The agent remembers application outcomes and re-ranks future matches.

## Salary band inference (SEEK hidden-range technique)

SEEK stores an internal salary range (`ranges.minimumAmount`/`maximumAmount`) on every ad purely for search filtering — it is never shown. "Salary undisclosed" ads still have this hidden range.

- If an ad displays a salary, use it directly (`salarySource` = `displayed`).
- Otherwise probe fixed salary-band queries via the `salaryrange=<min>-<max>` filter: an ad silently drops out of results when the filter crosses its hidden boundary. Record the contiguous band it appears in (`salarySource` = `inferred`).
- Accepted band buckets (configurable): `Under 80k`, `80-99k`, `100-119k`, `120-149k`, `150-199k`, `200k+`.
- Run as **background enrichment** (daily/on-demand), cache band snapshots, validate the method against known-salary ads (their displayed range must fall inside the inferred band).
- Rate-limit politely and cache aggressively; SEEK has no public API (undocumented search JSON) — anti-bot protection is a real risk.

## PII rule (non-negotiable)

**No PII ever reaches an LLM.** Not in prompts, tool results, embeddings, or logs. Enforced by placeholder substitution:

1. Before any model call, every known PII value (name, contact details, address, or any identifying data) is replaced with a stable placeholder (`[NAME]`, `[EMAIL]`, `[LOCATION_1]`, `[EMPLOYER_1]`, ...).
2. The real values + placeholder mapping live in the **database** (SQLite from 2.2; in-memory until then). Only placeholders go over the wire.
3. Responses are **rehydrated** (placeholders → real values) before anything is persisted or shown.

Owner commits: `1.2` placeholder engine (must land before `1.3`, the first commit to send profile-derived text to a model); mapping persistence in `2.2`. Job ads are public content and pass through unchanged. Applies to every Phase 1+ commit that touches model calls.

## Phased build plan

Each phase is independently shippable. Commits are one concern each; **pause after every commit** for user review (see review workflow).

### Phase 0 — "an agent actually runs" (hello world slice)
| # | Commit | What lands |
|---|--------|-----------|
| 0.1 | `chore: scaffold solution and repo conventions` | `.gitignore`, `.editorconfig`, solution, project folders, skills, PLAN.md |
| 0.2 | `feat: add ASP.NET Core minimal API project` | `JobHunt.Api` host (still empty of AI) |
| 0.3 | `feat: add React+Vite+TypeScript frontend` | `JobHunt.Web` SPA scaffold |
| 0.4 | `feat: wire config-switchable chat client (ollama default, OpenCode Zen optional)` | `AiOptions` + `.env` loader + `AddChatClient` over OpenAI-compatible endpoints |
| 0.5 | `feat: add JD Scanner agent returning structured analysis` | `AIAgent` + JSON output schema |
| 0.6 | `feat: add POST /api/scan endpoint` | API → agent wiring |
| 0.7 | `feat: add Scan page to React app` | paste JD → render analysis |
| 0.8 | `docs: add README with setup and layout` | run instructions |

### Phase 1 — Match scoring
| # | Commit | What lands |
|---|--------|-----------|
| 1.1 | `feat: add CandidateProfile model with sample profile` | sample/fictional profile in-repo; real one via gitignored config (`profile*.json`, see .gitignore) |
| 1.2 | `feat: add PII placeholder engine` | redact before model calls, rehydrate after; in-memory map (DB table lands in 2.2) |
| 1.3 | `feat: register agent tools GetProfile and ScoreJob` | `FunctionInvokingChatClient`; profile text passes through 1.2 redaction |
| 1.4 | `feat: add embedding similarity scoring` | `nomic-embed-text` via Ollama embedding generator |
| 1.5 | `feat: return structured match score in API` | breakdown: skills / seniority / fit |
| 1.6 | `feat: show match score breakdown in React UI` | score card |

### Phase 2 — Pipeline, job source, salary bands
| # | Commit | What lands |
|---|--------|-----------|
| 2.1 | `feat: add IJobSource abstraction and in-memory source` | connector interface (`IJobSource`), mock source for demos/tests |
| 2.2 | `feat: add Application model with EF Core SQLite` | persistence incl. `salaryBand` + `salarySource` (displayed/inferred); PII placeholder mapping table for 1.2 |
| 2.3 | `feat: add discovery and scoring pipeline` | discovered → scored → persisted |
| 2.4 | `feat: add salary band inference engine` | band-probing over a job source; grouping into bands |
| 2.5 | `feat: add tailoring agent for resume bullets and cover letter` | draft outputs per job |
| 2.6 | `feat: add human-in-the-loop approval API` | `AwaitingApproval → Applied` gate |
| 2.7 | `feat: add tracker board and approval queue (groupable by salary band)` | React board + "show 100–119k" grouping |

### Phase 3 — Memory & follow-ups
| # | Commit | What lands |
|---|--------|-----------|
| 3.1 | `feat: add outcome memory store` | EF-backed outcomes |
| 3.2 | `feat: re-rank scoring from outcome memory` | learning behavior |
| 3.3 | `feat: add follow-up reminder drafts` | for `Applied` items |

### Phase 4 — Public polish
| # | Commit | What lands |
|---|--------|-----------|
| 4.1 | `feat: add SEEK search connector` | live listings via SEEK search JSON; rate-limited + cached |
| 4.2 | `feat: run salary inference on live undisclosed SEEK ads` | reuses 2.4 engine; validates against known-salary ads |
| 4.3 | `feat: add OpenTelemetry tracing and health checks` | observability |
| 4.4 | `test: add XUnit suite for pipeline, scoring, and band inference` | tests |
| 4.5 | `ci: add GitHub Actions build and test workflow` | CI |
| 4.6 | `docs: finalize README with architecture and demo` | recruiter-facing |

~31 commits total; each one compiles and shows a visible increment.

## Review workflow (how we execute)

1. Present the next commit plan (what will land).
2. Implement it. Build to verify it compiles.
3. Commit with a Conventional Commit message (matching the planned title).
4. **Pause** — user reviews.
5. On "continue", repeat. Never bundle multiple commits without being asked.

Two skills enforce discipline: `jobhunt-agent` (this file, project plan) and `small-commits` (commit behavior).

## Rules

- Keep real candidate PII out of the repo (gitignored `profile*.json`); ship a `.sample` profile.
- **No PII in any LLM call** — substitute placeholders before prompts/embeddings/tool results, store real values + mapping in the database, rehydrate responses afterward (see "PII rule" above). Verify no model-call path bypasses redaction.
- Do not auto-apply to jobs — always wait for human approval.
- Respect SEEK rate limits / caching to avoid anti-bot blocks.
- Verify with a build before every commit.