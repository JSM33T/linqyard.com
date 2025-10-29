from __future__ import annotations

from typing import List, Optional

from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    question: str = Field(..., min_length=1, description="Natural language question from the user.")
    conversation_id: Optional[str] = Field(
        default=None,
        description="Optional identifier to continue an existing conversation thread.",
    )
    max_references: int = Field(
        default=3,
        ge=1,
        le=10,
        description="Maximum number of supporting documents to return with the answer.",
    )


class SourceDocument(BaseModel):
    source: str = Field(..., description="Path or identifier of the supporting document.")
    snippet: Optional[str] = Field(
        default=None,
        description="Short excerpt taken from the supporting document.",
    )
    score: Optional[float] = Field(
        default=None,
        description="Confidence score provided by the retriever, if available.",
    )


class LinkItem(BaseModel):
    label: str = Field(..., description="Short description of the link destination.")
    url: str = Field(..., description="URL that the client can navigate to.")


class ActionPayload(BaseModel):
    name: str = Field(..., description="Unique identifier for the action to perform.")
    method: str = Field(..., description="HTTP method or verb to use for the downstream call.")
    endpoint: str = Field(..., description="Relative or absolute endpoint for the action.")
    parameters: Optional[dict] = Field(
        default=None,
        description="Optional payload to send when executing the action.",
    )


class FollowUpField(BaseModel):
    name: str = Field(..., description="Machine-friendly identifier for the field (e.g. email).")
    label: str = Field(..., description="Human-readable label to show in the UI.")
    input_type: str = Field(
        ...,
        description="Expected input type such as text, email, password, otp.",
    )
    required: bool = Field(default=True, description="Whether the field is mandatory.")


class FollowUpRequest(BaseModel):
    prompt: str = Field(..., description="Message instructing the user what to provide next.")
    fields: List[FollowUpField] = Field(
        ...,
        description="List of fields that should be collected from the user.",
    )


class ChatResponse(BaseModel):
    answer: str = Field(..., description="LLM generated answer grounded in the knowledge base.")
    sources: List[SourceDocument] = Field(
        default_factory=list,
        description="Supporting documents used to compose the answer.",
    )
    conversation_id: Optional[str] = Field(
        default=None,
        description="Conversation identifier echoed back when provided in the request.",
    )
    response_type: str = Field(
        default="text",
        description="Type of answer payload such as text, text_with_links, text_with_action, or text_with_follow_up.",
    )
    links: List[LinkItem] = Field(
        default_factory=list,
        description="Optional list of links to surface alongside the answer.",
    )
    action: Optional[ActionPayload] = Field(
        default=None,
        description="Optional action that the client can perform as a next step.",
    )
    follow_up: Optional[FollowUpRequest] = Field(
        default=None,
        description="Optional follow-up instructions requesting more user input.",
    )
