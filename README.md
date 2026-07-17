# GrantOS Sentinel — Local Agent Desktop

A local-first AI knowledge and coding companion. Everything runs on your machine:
a Blazor Server app wrapped in Electron.NET talks to a local [Ollama](https://ollama.com)
server and stores all conversations, memory, prompts, and audit history in a single SQLite
file. No cloud, no API keys, no telemetry.

The original local-chat milestone is complete. Sentinel can also propose local actions
through model tool calls. Every action requires explicit approval, and dedicated filesystem
tools are restricted to configured directories.

## What's in the box

Clean Architecture, four layers plus tests:

```
src/
  GrantOS.Sentinel.Domain          entities + enums (no dependencies)
  GrantOS.Sentinel.Application      interfaces, options, Ollama DTOs
  GrantOS.Sentinel.Infrastructure   EF Core (SQLite), services, Ollama HTTP client, DI
  GrantOS.Sentinel.UI               shared Razor components and pages
  GrantOS.Sentinel.Web              Blazor Server host + Electron.NET + localhost API
  GrantOS.Sentinel.Maui             parked Mac Catalyst host (toolchain-blocked)
tests/
  GrantOS.Sentinel.Tests            xUnit: memory service + Ollama serialization
```

The dependency rule points inward: Web and Infrastructure depend on Application and
Domain; Domain depends on nothing. Ollama's HTTP contract lives in exactly one class
(`OllamaChatService`), so nothing else in the app knows how Ollama is shaped.

> One deliberate Phase 1 simplification: there is no separate `GrantOS.Sentinel.Api`
> project. The API endpoints live inside the Web project (`Endpoints/SentinelApiEndpoints.cs`)
> so there is a single app to run. Splitting them into their own host later is
> straightforward — see "Roadmap".

## Prerequisites

1. **.NET 10 SDK** — verify with `dotnet --version` (expect `10.x`).
   Download: https://dotnet.microsoft.com/download/dotnet/10.0
2. **Ollama** — install from https://ollama.com, then make sure the server is running:
   ```bash
   ollama serve
   ```
   In another terminal, pull at least the default model:
   ```bash
   ollama pull qwen2.5-coder
   ```
   The app seeds four model profiles (`qwen2.5-coder` as default, plus `qwen3`,
   `gemma3`, `deepseek-r1`). Pull whichever you want to use; the Models page shows
   what you actually have installed versus what's just a saved profile.
3. **Playwright Chromium** — required only for the browser-control tool. After restoring or
   building the project, install its matching browser binary once:
   ```bash
   dotnet run --project src/GrantOS.Sentinel.Web -- --install-playwright-browsers
   ```

## First-time setup

From the solution root (`GrantOS.Sentinel/`):

```bash
# 1. Restore and build.
dotnet build

# 2. Install the EF Core tools once (if you don't have them).
dotnet tool install --global dotnet-ef

# 3. Create the initial migration. This is REQUIRED before the first run,
#    because the app calls Database.MigrateAsync() on startup.
dotnet ef migrations add InitialCreate \
  --project src/GrantOS.Sentinel.Infrastructure \
  --startup-project src/GrantOS.Sentinel.Web
```

Why the migration step is manual: this project uses EF **migrations** (durable, versioned
schema changes) rather than `EnsureCreated()`. Migrations can't be pre-generated for you
without your local SDK, so you run the command above once. It creates a `Migrations/`
folder in the Infrastructure project. After that, every `dotnet run` applies pending
migrations automatically.

> **Prefer to skip migrations for now?** In `src/GrantOS.Sentinel.Web/Program.cs`, replace
> `await db.Database.MigrateAsync();` with `await db.Database.EnsureCreatedAsync();`. That
> creates the schema directly from the model with no migration files. It's fine for a quick
> start but you lose versioned schema history, and the two approaches don't mix cleanly —
> pick one. Migrations are recommended for anything you intend to keep.

## Run the Electron desktop app

```bash
dotnet run --project src/GrantOS.Sentinel.Web
```

The command opens the Electron window directly; it does not open a separate browser tab.
A SQLite file named `grantos-sentinel.db` appears in the Web project's working directory
on first run.

## Test

```bash
dotnet test
```

The tests don't need Ollama or a real database running — the memory tests use an in-memory
SQLite connection, and the serialization tests exercise the JSON contract directly.

## What should happen (Milestone 1 acceptance)

- **Dashboard** shows an Ollama status pill (online/offline), plus counts of installed
  models, conversations, and memory entries.
- **Chat**: pick a model and (optionally) a system prompt, type a message, and watch the
  reply stream in token by token. The thread is saved the moment you send the first message.
- **Conversations**: your saved threads are listed; open one to reload it, or delete it.
- **Memory**: create, edit, search, pin, and delete notes.
- **System Prompts**: manage prompts and set which one is the default.
- **Models**: see models actually installed in Ollama (live) alongside your saved profiles.
- **Agent tools**: supported Ollama models can propose shell commands and allowlisted file
  reads, writes, directory listings, and visible-browser actions. Each valid action waits
  for Approve or Deny.
- **Audit**: every chat and proposed tool action is recorded with success/failure.

If Ollama is offline, the app still loads — chat is disabled with a clear message, and the
dashboard/status reflect it rather than throwing.

## The localhost API

A small JSON API lives under `/api/sentinel` for the future VS Code extension. It is
**localhost-only and unauthenticated** in Phase 1, so don't expose it beyond loopback.

| Method | Route                     | Purpose                              |
|--------|---------------------------|--------------------------------------|
| GET    | `/api/sentinel/health`    | `{ status, ollama }`                 |
| GET    | `/api/sentinel/models`    | installed Ollama models              |
| POST   | `/api/sentinel/chat`      | non-streaming reply (thin proxy)     |
| GET    | `/api/sentinel/memory`    | list/search memory entries           |
| POST   | `/api/sentinel/memory`    | create a memory entry                |

Quick check once the app is running:

```bash
curl http://localhost:5217/api/sentinel/health
```

Note `/chat` here is a thin, non-streaming proxy and does **not** save history — the Blazor
UI is the path that persists conversations. That keeps the API contract minimal until a
real client needs more.

## Troubleshooting

- **The app starts but every screen is empty / a "no such table" error on startup.**
  You haven't created the migration yet. Run the `dotnet ef migrations add InitialCreate`
  command above, or switch to `EnsureCreatedAsync()` as described.
- **Dashboard says Ollama is offline.** Start it with `ollama serve` and confirm
  `curl http://localhost:11434/api/tags` returns JSON. If you changed the port, update
  `Ollama:BaseUrl` in `src/GrantOS.Sentinel.Web/appsettings.json`.
- **Chat fails with a model error.** The selected model isn't pulled. Run
  `ollama pull <model>` (e.g. `ollama pull qwen2.5-coder`).
- **`dotnet ef` not found.** Run `dotnet tool install --global dotnet-ef` and reopen your
  terminal so the tools path is picked up.
- **HTTPS certificate warning on first run.** Run `dotnet dev-certs https --trust` once.

## Agent security

Filesystem tools resolve symlinks and reject paths outside `Agent:AllowedDirectories`
before presenting an approval prompt. The default `.` entry means the process working
directory. Replace it with explicit absolute directories to grant access elsewhere, or set
it to an empty list to disable filesystem tools.

Shell commands are intentionally separate: they run as the current user and are not
restricted by the filesystem allowlist. Review the exact command shown in the approval
card before approving it.

Tool-calling support depends on the selected Ollama model. Sentinel queries the model's
capabilities and only sends tool definitions to compatible models. `qwen3:14b` was verified
locally; some models may support ordinary chat but not structured tool calls.

## Honest caveats

- The Electron/Blazor host builds and the automated test suite runs locally. The parked MAUI
  project still requires a Mac Catalyst workload compatible with the installed Xcode version.
- No authentication or work-repository indexing is implemented yet.

## Roadmap (beyond Milestone 1)

- Split the localhost API into its own `GrantOS.Sentinel.Api` host and add auth before it
  ever leaves loopback.
- Persist token counts and streaming metadata per message.
- Headed browser automation with a per-action approval flow.
- A short `sentinel` terminal launcher that focuses the Electron window.
- Knowledge base, project standards, and workspace indexing (the "soon" nav items).
- Retrieval over the memory vault to feed relevant notes into prompts automatically.

## Configuration reference

`src/GrantOS.Sentinel.Web/appsettings.json`:

```json
{
  "ConnectionStrings": { "Sentinel": "Data Source=grantos-sentinel.db" },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "qwen2.5-coder",
    "TimeoutSeconds": 300
  },
  "Sentinel": { "DefaultScope": "Personal" },
  "Agent": {
    "AllowedDirectories": ["."],
    "MaxReadBytes": 262144,
    "MaxWriteBytes": 1048576,
    "MaxDirectoryEntries": 200,
    "MaxBrowserTextCharacters": 50000,
    "BrowserTimeoutSeconds": 30,
    "BrowserHeadless": false
  }
}
```

No secrets live in configuration or source control. The `.gitignore` excludes the SQLite
database files.
