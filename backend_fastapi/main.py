from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from api.bot.api import router as bot_router
from fastapi.responses import HTMLResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from pathlib import Path
from dotenv import load_dotenv
import os

load_dotenv(Path(__file__).resolve().parents[1] / ".env")

app = FastAPI()

# Mount static directory
static_dir = Path(__file__).parent / "static"
app.mount("/static", StaticFiles(directory=static_dir), name="static")

allowed_origins = [
    "http://localhost:3000",
    "https://localhost:3000",
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.include_router(bot_router)


@app.get("/health")
async def health():
    # Read TEST environment variable (provided via Docker Compose env_file)
    test_val = os.environ.get("TEST")
    return {"status": "ok", "TEST": test_val}


@app.get("/", response_class=HTMLResponse)
async def root():
    index_file = static_dir / "index.html"
    if index_file.exists():
        return FileResponse(index_file)
    return HTMLResponse("<html><body><h1>Index not found</h1></body></html>")
