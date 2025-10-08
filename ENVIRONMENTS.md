Environment strategy for LinkyardMonoRepo
=========================================

This document explains how environment variables are handled for local development and production, how Docker Compose uses `.env` / `.env.development`, and what files should be ignored by Git.

1) Which files exist in this repo
---------------------------------
- `.env` — optional project-level environment file (not injected into containers automatically unless configured). Use for interpolation inside `docker-compose.yml`.
- `.env.development` — dev-only environment file. This repo currently uses `.env.development` and the `fastapi` service is configured with `env_file: .env.development`.
- `docker-compose.yml` — main compose file. Services may reference `env_file` to inject variables into containers.
- `docker-compose.override.yml` — local development override (bind mounts and dev server). This file should typically be in `.gitignore`.

2) How Docker Compose uses environment files
-------------------------------------------
- `.env` (at repository root) is read by Docker Compose for variable interpolation in `docker-compose.yml` itself (e.g. ${VARIABLE}). It is not automatically injected into containers.
- `env_file` under a service tells Compose to load those key/value pairs and inject them into the container environment. Example in this repo:

  services:
    fastapi:
      env_file:
        - .env.development

  That is why the FastAPI container saw `TEST="local_dev"` when running locally.

3) Local development workflow (recommended)
------------------------------------------
- Keep `.env.development` locally with dev secrets/values. Do NOT commit the real `.env.development` to Git.
- Use `docker-compose.override.yml` for bind-mounts and autoreload to speed development. Example contents are provided in `docker-compose.override.yml.example` (or already present in your local copy). This override runs `uvicorn --reload` and mounts `./backend_fastapi` into `/app`.

Start dev server (from repo root):

```powershell
docker compose up fastapi
```

4) Production workflow
----------------------
- Do NOT use `docker-compose.override.yml` in production. Build a proper image (CI) and deploy with production environment variables injected by your deployment platform (not via a committed `.env` file).

To build and run the production-style container locally (rebuild the image):

```powershell
docker compose build fastapi
docker compose up -d fastapi
```

This uses the `Dockerfile` which copies source into the image and runs gunicorn+uvicorn workers.

5) Git ignore recommendations
-----------------------------
Add the following to your repository `.gitignore` to avoid committing secrets or local-only files:

```
# Local dev override - keep local only
docker-compose.override.yml

# Environment files (do not commit real secrets)
.env.development
.env.local
.env
```

6) Safe examples for the repo
----------------------------
- Commit `docker-compose.override.yml.example` and `.env.example` with placeholder values and comments so new contributors know how to run the project without exposing secrets.

Example `.env.example`:
```
# Example env values for local development
TEST="local_dev"
```

7) Untracking an already-committed file
---------------------------------------
If you already committed `docker-compose.override.yml` or `.env.development`, untrack it while keeping the local file:

```powershell
git rm --cached docker-compose.override.yml
git rm --cached .env.development
git commit -m "Stop tracking local dev files"
```

8) Summary
----------
- Use `.env` for interpolation, `env_file` for injecting into containers.
- Keep dev-only files out of the repo and commit example/template files instead.
- Use `docker-compose.override.yml` and bind mounts for iterative local development.
