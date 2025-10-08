# syntax=docker/dockerfile:1.7
FROM python:3.12-slim AS base
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 PIP_NO_CACHE_DIR=1
WORKDIR /app

# Install system deps only if needed (e.g., for psycopg, mysqlclient, etc.)
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl ca-certificates build-essential && rm -rf /var/lib/apt/lists/*

# ----- builder: resolve and install deps -----
FROM base AS builder
COPY pyproject.toml poetry.lock* requirements*.txt* /app/
# If a requirements.txt exists, install from it. Otherwise use Poetry to export requirements.
RUN pip install --upgrade pip && \
    if [ -f /app/requirements.txt ]; then \
        pip install --prefix=/install -r /app/requirements.txt; \
    else \
        pip install poetry && \
        poetry export -f requirements.txt --output /app/req.txt --without-hashes && \
        pip install --prefix=/install -r /app/req.txt; \
    fi

# ----- runtime: copy only what we need -----
FROM base AS runtime
# Create non-root user
RUN useradd -u 10001 -m appuser
COPY --from=builder /install /usr/local
COPY . /app
USER appuser

# Expose uvicorn port
EXPOSE 8001

# Healthcheck (optional)
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s CMD curl -f http://localhost:8001/health || exit 1

# Start with gunicorn+uvicorn workers (good defaults; tune workers for CPU)
CMD ["gunicorn", "-k", "uvicorn.workers.UvicornWorker", "main:app", \
     "--bind", "0.0.0.0:8001", "--workers", "2", "--threads", "4", "--timeout", "60"]
