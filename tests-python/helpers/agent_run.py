"""Agent-run → evaluator-inputs adapter (SKELETON — you implement the bodies).

Turn an ``agent_framework`` ``AgentResponse`` into the fields the
``azure-ai-evaluation`` evaluators consume: ``query`` (user message),
``response`` (final assistant text), ``tool_calls`` and ``tool_definitions``.

Parity target: ``AgentRunResponseExtensions.ToChatResponse``.
Full reference: ``solutions-python/helpers/agent_run.py``.
"""

from __future__ import annotations

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


def to_eval_inputs(
    response: Any,
    *,
    query: str,
    tool_definitions: list[dict[str, Any]] | None = None,
) -> EvalInputs:
    """Adapt an ``agent.run()`` result into :class:`EvalInputs`.

    TODO(student):
      * read the final assistant text from ``response.text``
      * collect ``function_call`` content from ``response.messages[*].contents``
        (a content item is a tool call when ``content.type == "function_call"``;
        it exposes ``call_id``, ``name`` and ``arguments``)
      * return ``EvalInputs(query=query, response=<text>, tool_calls=[...],
        tool_definitions=list(tool_definitions or []))``
    """
    raise NotImplementedError("TODO(student): implement to_eval_inputs")


def write_jsonl(inputs: EvalInputs, path: str | Path) -> Path:
    """Write a single-row JSONL dataset for ``evaluate(data=...)``; return the path.

    TODO(student):
      * ensure the parent directory exists
      * write ``json.dumps(inputs.to_row())`` followed by a newline
      * return the ``Path``
    """
    raise NotImplementedError("TODO(student): implement write_jsonl")


__all__ = ["EvalInputs", "to_eval_inputs", "write_jsonl"]
