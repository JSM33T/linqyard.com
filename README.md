# Linqyard

This repository contains a small monorepo for the Linqyard project: a .NET 9 API, a FastAPI (Python) companion service, and a Next.js frontend. This README explains how to get the project running locally for development and production-style Docker runs.

## What's in this repo

- `backend_dotnet/` - .NET 9 API (Linqyard.Api and related projects)
- `backend_fastapi/` - Minimal FastAPI backend used for simple static content and health checks
- `frontend_nextjs/` - Next.js frontend app
- `docker-compose.yml` - Production-ish Compose file (build images and expose ports)
- `docker-compose.override.yml` / `docker-compose.override.example.yml` - Development overrides with bind mounts and hot reload

## Prerequisites

- Docker & Docker Compose (Desktop or Engine)
- Node.js 18+ and npm (for running frontend locally without Docker)
- .NET 9 SDK (if you want to run the .NET backend locally without Docker)
- Python 3.12+ and pip (if you want to run the FastAPI service locally without Docker)

Notes for Windows: run Docker Desktop and ensure Docker can bind to the required ports. When running the frontend in Docker, `host.docker.internal` is used to reach the host services (e.g. the .NET API mapped on host port 5000).

## Ports (defaults)

- .NET API (inside container): 8080 -> Host: 5000
- FastAPI (inside container): 8001 -> Host: 8001
- Next.js frontend: 3000 -> Host: 3000

## Quickstart — Docker (recommended for most contributors)

This uses the top-level `docker-compose.yml`. For development you can use the override which mounts local source and enables hot reload.

1. Copy or create a `.env` file at the repo root (Docker Compose references `.env`). The repository includes examples for env usage in compose; fill any required values.

2. Start services (build images then run):

```powershell
# Build and start all services in foreground
docker compose up --build

# Or run in detached mode
docker compose up --build -d
```

3. Visit the apps in your browser:

- Frontend: http://localhost:3000
- .NET API: http://localhost:5000
- FastAPI: http://localhost:8001

To stop and remove containers created by Compose:

```powershell
docker compose down
```

## Development with hot-reload (compose override)

The project includes `docker-compose.override.yml` which is intended for development. It bind-mounts local source into the containers and runs dotnet/watch, uvicorn with --reload, and Next.js dev server.

To use it (Compose automatically picks up `docker-compose.override.yml`):

```powershell
docker compose up --build
```

Behavior in the override:

- `.NET` service runs `dotnet watch run` and is reachable at http://localhost:5000
- `FastAPI` runs `uvicorn main:app --reload` at port 8001
- `Next.js` runs `npm run dev` and is reachable at http://localhost:3000; it is configured to call the backend via `NEXT_PUBLIC_API_URL=http://host.docker.internal:5000` by default in the override

## Run services individually (local, without Docker)

### FastAPI

From `backend_fastapi/` you can run the dev server directly:

```powershell
cd backend_fastapi
python -m venv .venv; .\.venv\Scripts\Activate; pip install -r requirements.txt
# Run uvicorn with reload
uvicorn main:app --host 0.0.0.0 --port 8001 --reload
```

Open http://localhost:8001 and check `/health` for status.

### .NET API

If you prefer to run the .NET API locally (recommended to have .NET SDK installed):

```powershell
cd backend_dotnet/Linqyard.Api
dotnet restore
dotnet watch run --urls "http://0.0.0.0:8080"
```

Then open http://localhost:5000 (if you prefer the same host port mapping as Docker, run the process behind a reverse proxy or adjust your local call to port 8080).

### Next.js frontend

From `frontend_nextjs/`:

```powershell
cd frontend_nextjs
npm install
npm run dev
```

By default the app runs on http://localhost:3000. If the frontend needs to talk to the backend running on your host, set `NEXT_PUBLIC_API_URL` accordingly (for example `http://localhost:5000` or `http://localhost:8080`).

## Environment variables

- Compose files reference a top-level `.env` file. Create one with any secrets or variables needed by services.
- The Next.js app reads `NEXT_PUBLIC_API_URL` to target the API.
- The FastAPI app in `backend_fastapi` reads `TEST` in the `/health` handler as an example of env usage.

## Logs and persistence

- The `docker-compose.yml` and override mount `./logs/dotnet` to persist .NET logs locally. Check `logs/dotnet` in the repo for container logs if enabled.

## Useful commands

```powershell
# Build images only
docker compose build

# Recreate a single service
docker compose up -d --no-deps --build nextjs

# View container logs (tail)
docker compose logs -f fastapi

# Run a one-off shell in a service
docker compose run --rm nextjs sh
```

## Migrations

Add migration

```powershell
dotnet ef migrations add "Initial Rebase" --project Linqyard.Data --startup-project Linqyard.Api
```

Update Database

```powershell
dotnet ef database update --project Linqyard.Data --startup-project Linqyard.Api
```

## Troubleshooting

- If ports are in use, confirm no other local service is bound to 3000, 5000, or 8001.
- On Windows, if Docker tooling has networking issues, restart Docker Desktop.
- If Next.js can't reach the API from inside Docker, check the `NEXT_PUBLIC_API_URL` (use `host.docker.internal` for host access from Linux/Windows/Mac Docker Desktop).

## Next steps / Contributions

- Add a `.env.example` with the minimal environment variables your team expects.
- Consider adding service health endpoints and docker-compose healthchecks for better orchestration.

---
Generated: initial setup guide (automatically created).