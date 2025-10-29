"use client";

import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Bot,
  ExternalLink,
  Loader2,
  MessageCircle,
  Send,
  Sparkles,
  UserRound,
  X,
  Zap,
} from "lucide-react";

type LinkItem = {
  label: string;
  url: string;
};

type ActionPayload = {
  name: string;
  method: string;
  endpoint: string;
  parameters?: Record<string, unknown> | null;
};

type FollowUpField = {
  name: string;
  label: string;
  input_type: string;
  required: boolean;
};

type FollowUpRequest = {
  prompt: string;
  fields: FollowUpField[];
};

type SourceDocument = {
  source: string;
  snippet?: string | null;
};

type BotApiResponse = {
  answer: string;
  sources: SourceDocument[];
  conversation_id?: string | null;
  response_type?: string | null;
  links?: LinkItem[] | null;
  action?: ActionPayload | null;
  follow_up?: FollowUpRequest | null;
};

type ChatMessage = {
  id: string;
  role: "user" | "assistant";
  text: string;
  responseType?: string;
  links?: LinkItem[];
  action?: ActionPayload | null;
  followUp?: FollowUpRequest | null;
  sources?: SourceDocument[];
  pending?: boolean;
  error?: boolean;
};

const BOT_API_BASE =
  (process.env.NEXT_PUBLIC_BOT_API_URL ||
    "http://localhost:8000").replace(/\/$/, "");

const CHAT_ENDPOINT = `${BOT_API_BASE}/api/bot/chat`;

function generateId() {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

const INITIAL_PROMPT =
  "Hi! I'm the Linqyard assistant. Ask me anything about onboarding, integrations, or troubleshooting.";

export function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: generateId(),
      role: "assistant",
      text: INITIAL_PROMPT,
      responseType: "text",
    },
  ]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [isSending, setIsSending] = useState(false);
  const [actionStatus, setActionStatus] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  const toggleWidget = useCallback(() => {
    setIsOpen((prev) => !prev);
  }, []);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const container = scrollRef.current;
    if (!container) {
      return;
    }
    container.scrollTop = container.scrollHeight;
  }, [messages, isOpen]);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      const trimmed = input.trim();
      if (!trimmed || isSending) {
        return;
      }

      const userMessage: ChatMessage = {
        id: generateId(),
        role: "user",
        text: trimmed,
      };
      setMessages((prev) => [...prev, userMessage]);
      setInput("");
      setIsSending(true);

      try {
        const response = await fetch(CHAT_ENDPOINT, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            question: trimmed,
            conversation_id: conversationId ?? undefined,
          }),
        });

        if (!response.ok) {
          throw new Error(`Chat API returned ${response.status}`);
        }
        const data: BotApiResponse = await response.json();
        setConversationId((prev) => data.conversation_id ?? prev ?? null);

        const assistantMessage: ChatMessage = {
          id: generateId(),
          role: "assistant",
          text: data.answer ?? "I could not generate a response just now.",
          responseType: data.response_type ?? "text",
          links: data.links ?? undefined,
          action: data.action ?? null,
          followUp: data.follow_up ?? null,
          sources: data.sources ?? [],
        };
        setMessages((prev) => [...prev, assistantMessage]);
      } catch (error) {
        console.error("Chat request failed", error);
        setMessages((prev) => [
          ...prev,
          {
            id: generateId(),
            role: "assistant",
            text: "Sorry, I ran into a problem answering that. Please try again.",
            responseType: "text",
            error: true,
          },
        ]);
      } finally {
        setIsSending(false);
      }
    },
    [input, isSending, conversationId],
  );

  const handleActionTrigger = useCallback(
    async (action: ActionPayload | null | undefined) => {
      if (!action) {
        return;
      }
      setActionStatus("Working on your request…");
      const target =
        action.endpoint.startsWith("http") || action.endpoint.startsWith("https")
          ? action.endpoint
          : `${BOT_API_BASE}${action.endpoint.startsWith("/") ? action.endpoint : `/${action.endpoint}`}`;
      try {
        const response = await fetch(target, {
          method: action.method?.toUpperCase() ?? "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: action.parameters ? JSON.stringify(action.parameters) : undefined,
        });
        if (!response.ok) {
          throw new Error(`Action API returned ${response.status}`);
        }
        setActionStatus("Diagnostic requested successfully.");
      } catch (error) {
        console.error("Action request failed", error);
        setActionStatus("We could not trigger that action. Please try again later.");
      } finally {
        setTimeout(() => setActionStatus(null), 4000);
      }
    },
    [],
  );

  const footerHint = useMemo(() => {
    if (isSending) {
      return "Thinking...";
    }
    if (actionStatus) {
      return actionStatus;
    }
    return "Powered by the Linqyard knowledge base.";
  }, [isSending, actionStatus]);

  return (
    <div className="fixed bottom-4 right-4 z-[60] flex flex-col items-end gap-2">
      {isOpen && (
        <div className="w-80 sm:w-96 rounded-2xl border border-zinc-800 bg-zinc-950/90 text-zinc-100 shadow-xl backdrop-blur">
          <header className="flex items-center justify-between gap-2 rounded-t-2xl border-b border-zinc-800 bg-zinc-900/60 px-4 py-3">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500/20 text-emerald-400">
                <Bot className="h-4 w-4" aria-hidden="true" />
              </div>
              <div>
                <p className="text-sm font-semibold">Linqyard Assistant</p>
                <p className="text-xs text-zinc-400">Available 24/7</p>
              </div>
            </div>
            <button
              type="button"
              onClick={toggleWidget}
              className="inline-flex h-8 w-8 items-center justify-center rounded-full text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
              aria-label="Close chat"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          </header>

          <div ref={scrollRef} className="max-h-80 space-y-3 overflow-y-auto px-4 py-3 text-sm">
            {messages.map((message) => (
              <div
                key={message.id}
                className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}
              >
                <div
                  className={`flex max-w-full flex-col gap-2 rounded-2xl px-3 py-2 ${
                    message.role === "user"
                      ? "bg-emerald-500 text-zinc-950"
                      : "bg-zinc-900 text-zinc-100"
                  }`}
                >
                  <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-zinc-400">
                    {message.role === "user" ? (
                      <>
                        <UserRound className="h-3 w-3" aria-hidden="true" />
                        You
                      </>
                    ) : (
                      <>
                        <Sparkles className="h-3 w-3 text-emerald-400" aria-hidden="true" />
                        Assistant
                      </>
                    )}
                  </div>
                  <p className="whitespace-pre-line text-sm">{message.text}</p>

                  {message.links && message.links.length > 0 && (
                    <div className="space-y-1">
                      <p className="text-xs font-semibold uppercase text-emerald-300">Helpful links</p>
                      <ul className="space-y-1 text-xs">
                        {message.links.map((link) => (
                          <li key={`${message.id}-${link.url}`}>
                            <a
                              href={link.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1 text-emerald-300 hover:text-emerald-200"
                            >
                              <ExternalLink className="h-3 w-3" aria-hidden="true" />
                              {link.label}
                            </a>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {message.action && (
                    <div className="rounded-xl border border-emerald-500/40 bg-emerald-400/10 p-3 text-xs text-emerald-200">
                      <div className="mb-2 flex items-center gap-2 font-semibold uppercase">
                        <Zap className="h-3 w-3" aria-hidden="true" />
                        Suggested action
                      </div>
                      <p className="mb-2 break-all text-emerald-100">
                        {message.action.method} {message.action.endpoint}
                      </p>
                      <button
                        type="button"
                        onClick={() => handleActionTrigger(message.action)}
                        className="inline-flex items-center gap-2 rounded-full bg-emerald-500 px-3 py-1.5 font-medium text-emerald-950 hover:bg-emerald-400"
                      >
                        Trigger action
                      </button>
                    </div>
                  )}

                  {message.followUp && (
                    <div className="rounded-xl border border-zinc-700 bg-zinc-800/60 p-3 text-xs text-zinc-200">
                      <p className="mb-2 font-semibold uppercase text-zinc-300">Next we need</p>
                      <p className="mb-3 text-zinc-100">{message.followUp.prompt}</p>
                      <ul className="flex flex-wrap gap-2">
                        {message.followUp.fields.map((field) => (
                          <li
                            key={`${message.id}-${field.name}`}
                            className="rounded-full border border-zinc-600 px-2 py-1 text-[11px] uppercase tracking-wide text-zinc-300"
                          >
                            {field.label} · {field.input_type}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}


                  {message.sources && message.sources.length > 0 && (
                    <div className="border-t border-zinc-800 pt-2 text-[11px] uppercase tracking-wide text-zinc-500">
                      References:{" "}
                      {message.sources
                        .slice(0, 3)
                        .map((source) => source.source)
                        .join(", ")}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

          <form onSubmit={handleSubmit} className="border-t border-zinc-800 bg-zinc-900/80 px-4 py-3">
            <div className="flex items-center gap-2">
              <input
                value={input}
                onChange={(event) => setInput(event.target.value)}
                placeholder="Ask about integrations, onboarding, or support…"
                className="flex-1 rounded-full border border-transparent bg-zinc-800 px-3 py-2 text-sm text-zinc-100 placeholder:text-zinc-500 focus:border-emerald-500 focus:bg-zinc-900 focus:outline-none"
                disabled={isSending}
              />
              <button
                type="submit"
                className="inline-flex h-9 w-9 items-center justify-center rounded-full bg-emerald-500 text-emerald-950 hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={isSending || !input.trim()}
                aria-label="Send message"
              >
                {isSending ? (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                ) : (
                  <Send className="h-4 w-4" aria-hidden="true" />
                )}
              </button>
            </div>
            <p className="mt-2 text-[11px] uppercase tracking-wide text-zinc-500">{footerHint}</p>
          </form>
        </div>
      )}

      <button
        type="button"
        onClick={toggleWidget}
        className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-emerald-500 text-emerald-950 shadow-lg shadow-emerald-500/25 transition hover:scale-105 hover:bg-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-200 focus:ring-offset-2 focus:ring-offset-zinc-950"
        aria-label={isOpen ? "Minimise chat" : "Open chat"}
      >
        {isOpen ? (
          <X className="h-5 w-5" aria-hidden="true" />
        ) : (
          <MessageCircle className="h-5 w-5" aria-hidden="true" />
        )}
      </button>
    </div>
  );
}

export default ChatWidget;
