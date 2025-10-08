# Backend FastAPI (minimal)

This folder contains a minimal FastAPI app with a `/health` endpoint and a static `index.html`.

Running with Docker Compose (recommended):

1. From repository root run (PowerShell):

```powershell
# Build only the fastapi service
docker compose build fastapi

# Start the fastapi service in background
docker compose up -d fastapi
```

2. Confirm the service is running and view logs:

```powershell
docker compose logs -f fastapi
```

3. Verify the `/health` endpoint returns the TEST env var (served on port 8000):

```powershell
curl http://127.0.0.1:8001/health
```

Expected JSON: {"status": "ok", "TEST": "<value from .env.development>"}

Notes:
- Docker Compose reads environment variables from `.env.development` (configured in the repo root). Ensure it contains a TEST key, for example: `TEST="local_dev"`.
- The Dockerfile starts the app with gunicorn+uvicorn workers and includes a healthcheck.

TRIGGER DEPLOYMENT 8
