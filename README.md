<div align="center">
  <img src="frontend/src/lib/assets/Quota.png" alt="Quota logo" width="120" />
  <h1>Randware Quota</h1>
  <p>Capture the moments people quote for years, then surface them with a clean, fast dashboard.</p>
</div>

Quota is a Discord quote bot with a web dashboard for managing, browsing, and celebrating the best moments in your server. Save quotes with slash commands, let your community vote, and track the greatest hits without leaving Discord.

## Why Quota

- Designed for real communities: collect quotes fast and keep them organized.
- Built-in voting and stats so the best moments rise to the top.
- A polished dashboard for search, permissions, and server-level control.
- Simple setup: generate config, run the server, invite the bot.

## Repository layout

- `backend/` — .NET 10 solution with API, bot, database, and a config setup TUI
- `frontend/` — SvelteKit dashboard and landing site

## Prerequisites

- .NET 10 SDK
- Node.js (LTS recommended) and a package manager (`pnpm` preferred)
- A Discord application + bot credentials

## Quick start

### 1) Backend configuration

From `backend/`, run the interactive setup to generate a `config.toml`:

```bash
dotnet run --project ConfigSetupTUI
```

Copy the output into `backend/config.toml`.

Minimal config shape:

```toml
[jwt]
secret = "your_jwt_secret"
issuer = "your_issuer"
audience = "your_audience"
expiryMinutes = 5

[oauth]
id = "your_discord_client_id"
secret = "your_discord_secret"
apiEndpoint = "https://discord.com/api/v10"

[bot]
token = "your_bot_token"

[openapi]
enabled = true

[server]
port = 5000
```

`config.toml` is gitignored by design.

### 2) Frontend environment

Copy the example env file and fill it in:

```bash
cp frontend/.env.example frontend/.env
```

Required keys:

- `DISCORD_CLIENT_ID`
- `DISCORD_REDIRECT_URI`
- `BACKEND_HOST` (e.g. `http://localhost:5000`)

### 3) Install frontend deps

```bash
cd frontend
pnpm install
```

### 4) Run locally

Start the backend (from `backend/`):

```bash
dotnet run --project Main
```

Start the frontend (from `frontend/`):

```bash
pnpm dev
```

The backend will create a local SQLite database at `backend/database.db` on first run.

## Docker (frontend + backend + nginx)

This repo ships a `docker-compose.yml` to run both services behind an Nginx reverse proxy.

Create `backend/config.toml`, then copy the env file and fill it in:

```bash
cp .env.example .env
```

Run:

```bash
docker compose up --build
```

Nginx listens on `http://localhost:8080` and proxies:

- `/` → frontend
- `/api/` → backend

## Commands

### Backend (.NET)

Run the config wizard:

```bash
dotnet run --project ConfigSetupTUI
```

Run API + bot:

```bash
dotnet run --project Main
```

Build / restore:

```bash
dotnet restore
dotnet build
```

Optional Nix dev shell (from `backend/`):

```bash
nix develop
```

### Frontend (SvelteKit)

All scripts are in `frontend/package.json`:

```bash
pnpm dev          # start dev server
pnpm build        # production build
pnpm preview      # preview production build
pnpm check        # typecheck + svelte-check
pnpm check:watch  # watch mode
pnpm lint         # eslint + prettier
pnpm format       # format with prettier
```

## API documentation

If `[openapi] enabled = true` in `config.toml`, Swagger UI is available at:

```
http://localhost:{port}/swagger
```

## Discord bot permissions

Required permissions:

- View Channels
- Send Messages
- Send Messages in Threads
- Embed Links
- Use External Emojis
- Add Reactions
- View Guild Insights
- Manage Guild

You can also hit the backend endpoint `/bot/invite` to get an invite URL with the required permissions pre-configured.

## Notes

- Keep secrets out of version control (`backend/config.toml`, `frontend/.env`).
- The backend reads config from `backend/config.toml` at startup.
