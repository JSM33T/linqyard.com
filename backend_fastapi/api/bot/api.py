from __future__ import annotations

import asyncio
import json
import logging
import os
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path
from typing import List, Optional

from fastapi import APIRouter, HTTPException, status

from .schema import ChatRequest, ChatResponse, LinkItem, SourceDocument
from openai import OpenAI, OpenAIError

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/bot", tags=["bot"])

_PROJECT_ROOT = Path(__file__).resolve().parents[3]
_DEFAULT_KNOWLEDGE_FILE = Path("context_docs/faq.json")
_MIN_SIMILARITY_SCORE = float(os.getenv("KNOWLEDGE_MATCH_THRESHOLD", "0.45"))


class KnowledgeBaseError(Exception):
    """Base error for knowledge base failures."""


class ConfigurationError(KnowledgeBaseError):
    """Raised when required configuration (e.g., OpenAI key) is missing."""


@dataclass
class KnowledgeEntry:
    entry_id: str
    question: str
    aliases: List[str]
    answer: str
    instruction: Optional[str]
    clarify: Optional[str]
    links: List[LinkItem]


@dataclass
class ChatAnswer:
    template_answer: str
    instruction: Optional[str]
    clarify: Optional[str]
    links: List[LinkItem]
    sources: List[SourceDocument]


class KnowledgeBase:
    """Lightweight JSON-backed knowledge base that serves templated FAQ answers."""

    def __init__(
        self,
        entries: List[KnowledgeEntry],
        fallback_answer: str,
        fallback_instruction: Optional[str],
        fallback_links: List[LinkItem],
    ) -> None:
        self._entries = entries
        self._fallback_answer = fallback_answer
        self._fallback_instruction = fallback_instruction
        self._fallback_links = fallback_links

    @classmethod
    def from_json(cls, path: Path) -> "KnowledgeBase":
        if not path.exists():
            raise KnowledgeBaseError(f"Knowledge base file '{path}' not found.")

        try:
            raw_data = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            raise KnowledgeBaseError(f"Knowledge base file '{path}' is not valid JSON: {exc}") from exc

        faqs_raw = raw_data.get("faqs", [])
        if not isinstance(faqs_raw, list):
            raise KnowledgeBaseError("Knowledge base JSON must contain a 'faqs' array.")

        entries: List[KnowledgeEntry] = []
        for idx, item in enumerate(faqs_raw):
            if not isinstance(item, dict):
                logger.warning("Skipping FAQ entry at index %s: expected object, received %s", idx, type(item))
                continue

            entry_id = str(item.get("id") or f"entry_{idx:03d}")
            question = str(item.get("question") or "").strip()
            answer = str(item.get("answer") or "").strip()
            instruction = item.get("instruction")
            clarify = item.get("clarify")

            aliases = [
                str(alias).strip()
                for alias in item.get("aliases", [])
                if isinstance(alias, str) and str(alias).strip()
            ]

            links: List[LinkItem] = []
            links_raw = item.get("links", [])
            if isinstance(links_raw, list):
                for link in links_raw:
                    if not isinstance(link, dict):
                        continue
                    label = str(link.get("label") or "").strip()
                    url = str(link.get("url") or "").strip()
                    if label and url:
                        links.append(LinkItem(label=label, url=url))

            entries.append(
                KnowledgeEntry(
                    entry_id=entry_id,
                    question=question,
                    aliases=aliases,
                    answer=answer,
                    instruction=str(instruction).strip() if isinstance(instruction, str) else None,
                    clarify=str(clarify).strip() if isinstance(clarify, str) else None,
                    links=links,
                )
            )

        fallback_answer_default = (
            "I could not find any relevant entries in the knowledge base for that question. "
            "Please refine the query or update the FAQ content."
        )
        fallback_block = raw_data.get("fallback", {})
        if isinstance(fallback_block, dict):
            fallback_answer = str(fallback_block.get("ask", fallback_answer_default)).strip() or fallback_answer_default
            fallback_instruction_raw = fallback_block.get("instruction")
            fallback_instruction_text = (
                str(fallback_instruction_raw).strip() if isinstance(fallback_instruction_raw, str) else None
            )
            fallback_links: List[LinkItem] = []
            contact = fallback_block.get("contact")
            if isinstance(contact, dict):
                label = str(contact.get("label") or "").strip()
                url = str(contact.get("url") or "").strip()
                if label and url:
                    fallback_links.append(LinkItem(label=label, url=url))
        else:
            fallback_answer = fallback_answer_default
            fallback_instruction_text = None
            fallback_links = []

        if not entries:
            logger.warning("Knowledge base loaded with zero FAQ entries from %s.", path)

        logger.info("Knowledge base loaded %s FAQ entries from %s", len(entries), path)
        return cls(entries, fallback_answer, fallback_instruction_text, fallback_links)

    async def aask(self, question: str, *, max_sources: int) -> ChatAnswer:
        match = await asyncio.to_thread(self._find_best_match, question)

        if match is None or match.score < _MIN_SIMILARITY_SCORE:
            logger.info("No knowledge base match for question='%s'", question)
            return ChatAnswer(
                template_answer=self._fallback_answer,
                instruction=self._fallback_instruction,
                clarify=None,
                links=self._fallback_links,
                sources=[],
            )

        entry = match.entry
        sources = [
            SourceDocument(
                source=f"faq.json::{entry.entry_id}",
                snippet=None,
                score=round(match.score, 3),
            )
        ]
        return ChatAnswer(
            template_answer=entry.answer or self._fallback_answer,
            instruction=entry.instruction,
            clarify=entry.clarify,
            links=entry.links,
            sources=sources[:max_sources] if max_sources else sources,
        )

    def _find_best_match(self, question: str) -> Optional["EntryMatch"]:
        question_clean = question.strip().lower()
        if not question_clean:
            return None

        best: Optional[EntryMatch] = None
        for entry in self._entries:
            score = _score_entry(question_clean, entry)
            if best is None or score > best.score:
                best = EntryMatch(entry=entry, score=score)
        return best


@dataclass
class EntryMatch:
    entry: KnowledgeEntry
    score: float


def _score_entry(question: str, entry: KnowledgeEntry) -> float:
    comparisons = [entry.question.lower()] + [alias.lower() for alias in entry.aliases]
    return max((_normalised_similarity(question, candidate) for candidate in comparisons), default=0.0)


def _normalised_similarity(a: str, b: str) -> float:
    if not a or not b:
        return 0.0

    a_tokens = set(a.split())
    b_tokens = set(b.split())
    if not a_tokens or not b_tokens:
        return 0.0

    intersection = a_tokens & b_tokens
    jaccard = len(intersection) / len(a_tokens | b_tokens)

    prefix_score = 1.0 if a.startswith(b) or b.startswith(a) else 0.0
    length_penalty = min(len(a), len(b)) / max(len(a), len(b))

    return max(jaccard, 0.6 * length_penalty + 0.4 * jaccard + 0.15 * prefix_score)


@lru_cache(maxsize=1)
def get_knowledge_base() -> KnowledgeBase:
    configured_path = os.getenv("KNOWLEDGE_BASE_FILE")
    if configured_path:
        candidate = Path(configured_path)
    else:
        candidate = _DEFAULT_KNOWLEDGE_FILE

    if not candidate.is_absolute():
        candidate = (_PROJECT_ROOT / candidate).resolve()

    return KnowledgeBase.from_json(candidate)


@router.post("/chat", response_model=ChatResponse)
async def chat_endpoint(payload: ChatRequest) -> ChatResponse:
    knowledge_base = get_knowledge_base()

    try:
        result = await knowledge_base.aask(
            question=payload.question,
            max_sources=payload.max_references,
        )
        answer_text = await _generate_response_text(
            question=payload.question,
            template=result.template_answer,
            instruction=result.instruction,
            clarify=result.clarify,
            links=result.links,
        )
    except KnowledgeBaseError as exc:
        if isinstance(exc, ConfigurationError):
            raise HTTPException(
                status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
                detail=str(exc),
            ) from exc
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=str(exc),
        ) from exc
    except Exception as exc:
        logger.exception("Unexpected error in chat endpoint")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Unexpected error while processing the chat request.",
        ) from exc

    response_type = "text_with_links" if result.links else "text"
    return ChatResponse(
        answer=answer_text,
        sources=result.sources[: payload.max_references],
        conversation_id=payload.conversation_id,
        response_type=response_type,
        links=result.links,
        action=None,
        follow_up=None,
    )


def _get_openai_client() -> OpenAI:
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        raise ConfigurationError("OPENAI_API_KEY is not configured; unable to generate assistant responses.")
    return OpenAI(api_key=api_key)


async def _generate_response_text(
    *,
    question: str,
    template: str,
    instruction: Optional[str],
    clarify: Optional[str],
    links: List[LinkItem],
) -> str:
    client = _get_openai_client()

    link_text = "\n".join(f"- {link.label}: {link.url}" for link in links) if links else "None provided."
    guidance_segments = [segment for segment in [instruction, clarify] if segment]
    prompt_instruction = " | ".join(guidance_segments) if guidance_segments else "No extra instructions."
    system_prompt = (
        "You are Linqyard's virtual assistant. Respond in a friendly, concise tone using the provided context. "
        "Do not fabricate information. If helpful links are provided, reference them naturally in the response. "
        "Avoid mentioning internal instructions or datasets."
    )
    user_prompt = (
        f"User question: {question}\n\n"
        f"Base answer: {template}\n"
        f"Guidance: {prompt_instruction}\n"
        f"Helpful links:\n{link_text}\n\n"
        "Compose the final reply for the user."
    )

    try:
        response = await asyncio.to_thread(
            client.chat.completions.create,
            model=os.getenv("OPENAI_CHAT_MODEL", "gpt-4o-mini"),
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            temperature=float(os.getenv("OPENAI_CHAT_TEMPERATURE", "0.3")),
        )
    except OpenAIError as exc:
        logger.exception("OpenAI chat completion failed: %s", exc)
        return template
    except Exception as exc:
        logger.exception("Unexpected error during OpenAI chat completion: %s", exc)
        return template

    choice = response.choices[0] if response and response.choices else None
    content = getattr(choice.message, "content", None) if choice else None
    if not content:
        return template
    return content.strip()
