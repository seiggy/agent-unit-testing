"""Agent-run → evaluator-inputs adapter (SOLUTION reference).

Python-idiomatic equivalent of ``AgentRunResponseExtensions.ToChatResponse``:
turn an ``agent_framework`` :class:`AgentResponse` into the fields the
``azure-ai-evaluation`` evaluators consume — ``query`` (the user message),
``response`` (final assistant text), ``tool_calls`` and ``tool_definitions``.

Also provides :func:`write_jsonl` to persist a single evaluation row for the
batch ``evaluate(...)`` upload to the Azure AI Foundry portal.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class EvalInputs:
    """Normalised inputs for the ``azure-ai-evaluation`` evaluators."""

    query: str
    response: str
    tool_calls: list[dict[str, Any]] = field(default_factory=list)
    tool_definitions: list[dict[str, Any]] = field(default_factory=list)

    def to_row(self) -> dict[str, Any]:
        """Return one JSONL row keyed to the evaluator parameter names."""
        return {
            "query": self.query,
            "response": self.response,
            "tool_calls": self.tool_calls,
            "tool_definitions": self.tool_definitions,
        }


def _extract_response_text(response: Any) -> str:
    """Best-effort final assistant text from an ``AgentResponse``."""
    text = getattr(response, "text", None)
    if text:
        return str(text).strip()
    # Fallback: concatenate text content across messages.
    parts: list[str] = []
    for message in getattr(response, "messages", None) or []:
        message_text = getattr(message, "text", None)
        if message_text:
            parts.append(str(message_text))
    return "\n".join(parts).strip()


def _extract_tool_calls(response: Any) -> list[dict[str, Any]]:
    """Collect ``function_call`` content across the response messages.

    Shape matches the OpenAI/Azure ``tool_call`` contract so the data lines up
    with the ``tool_definitions`` returned by ``us1_agent.get_tool_definitions``.
    """
    tool_calls: list[dict[str, Any]] = []
    for message in getattr(response, "messages", None) or []:
        for content in getattr(message, "contents", None) or []:
            if getattr(content, "type", None) != "function_call":
                continue
            tool_calls.append(
                {
                    "type": "tool_call",
                    "tool_call_id": getattr(content, "call_id", None),
                    "name": getattr(content, "name", None),
                    "arguments": getattr(content, "arguments", None),
                }
            )
    return tool_calls


def to_eval_inputs(
    response: Any,
    *,
    query: str,
    tool_definitions: list[dict[str, Any]] | None = None,
) -> EvalInputs:
    """Adapt an ``agent.run()`` result into :class:`EvalInputs`.

    :param response: the awaited ``AgentResponse`` from ``agent.run(query)``.
    :param query: the original user message (not always echoed in ``response``).
    :param tool_definitions: JSON-schema tool defs (``get_tool_definitions()``).
    """
    return EvalInputs(
        query=query,
        response=_extract_response_text(response),
        tool_calls=_extract_tool_calls(response),
        tool_definitions=list(tool_definitions or []),
    )


def write_jsonl(inputs: EvalInputs, path: str | Path) -> Path:
    """Write a single-row JSONL dataset for ``evaluate(data=...)``; return the path."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as handle:
        handle.write(json.dumps(inputs.to_row()) + "\n")
    return path


__all__ = ["EvalInputs", "to_eval_inputs", "write_jsonl"]
