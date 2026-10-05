# JobHuntAgent

A job-search automation agent: it searches job listings, matches them against a candidate profile, infers hidden salary ranges, shortlists the strongest fits, and tracks applications end-to-end — with human sign-off before anything is ever submitted.

**Current status: Phase 0** — paste a job description into the web page, a local LLM scans it, and you get a structured breakdown (title, company, seniority, work model, salary text, required/nice-to-have skills, responsibilities, benefits, keywords).

The full roadmap lives in [PLAN.md](PLAN.md).

## Stack

| Piece | Choice |
|-------|--------|
| Backend | ASP.NET Core Minimal API (.NET 10) |
| Agent orchestration | Microsoft Agent Framework (MAF) on Microsoft.Extensions.AI |
| Chat provider | ollama (default) or OpenCode Zen — one config switch; both speak the OpenAI API via Microsoft.Extensions.AI.OpenAI |
| Frontend | React 19 + TypeScript (Vite), oxlint |
| Persistence | EF Core + SQLite (Phase 2) |

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

Open <http://localhost:5173>, paste a job description, and hit **Scan job description**. On a CPU-only machine with `gemma3`, expect roughly a minute per scan.

Try the API directly:

```bash
curl -X POST http://localhost:5051/api/scan \
  -H "Content-Type: text/plain" \
  --data-binary @job-description.txt
```

## Configuration

All settings live under the `AI` section of `src/JobHunt.Api/appsettings.json` and can be overridden with environment variables using .NET's `__` separator (`AI__Provider=zen`).

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

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/` | liveness |
| `GET` | `/api/ai/ping` | round-trips a trivial prompt through the configured chat model |
| `POST` | `/api/scan` | scans a raw job description (request body = text) and returns the structured analysis |

## Layout

```
src/
  JobHunt.Api/            ASP.NET Core Minimal API
    Ai/                   config-switchable IChatClient wiring, .env loader
    Features/JdScan/      the JD Scanner agent + result model + JSON parser
    Program.cs            endpoints + DI composition root
  JobHunt.Web/            React + Vite frontend
    src/scan.ts           typed client for POST /api/scan
    src/ScanPage.tsx      the Scan page
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

Phases 1–4 in [PLAN.md](PLAN.md): match scoring against a candidate profile, a discovery → scoring → tailoring → approval → tracking pipeline with salary-band inference, outcome memory, then live SEEK search, OpenTelemetry, and a test suite.