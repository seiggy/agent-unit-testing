"""Foundry / Azure OpenAI config resolver (SKELETON — you implement the bodies).

Build the **judge** ``AzureOpenAIModelConfiguration`` the ``azure-ai-evaluation``
evaluators use. The judge deployment is the lab's ``chat`` deployment (the same
model the agent uses) and auth is keyless via ``DefaultAzureCredential``.

Parity target: ``FoundryConnectionStringParser.cs`` (``FoundryConnectionStringParts``).
Full reference: ``solutions-python/helpers/foundry_config.py``.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from agent_evals_workshop.config import Settings

_DEPLOYMENT_KEYS = ("deployment", "deploymentid", "model")
_ENDPOINT_KEYS = ("endpointaiinference", "endpoint")


@dataclass(frozen=True)
class FoundryConnectionParts:
    """Typed view over a parsed Foundry connection string."""

    endpoint: str | None = None
    deployment: str | None = None
    api_key: str | None = None
    api_version: str | None = None


def parse_connection_string(connection_string: str | None) -> FoundryConnectionParts:
    """Parse a ``Key=Value;...`` connection string into :class:`FoundryConnectionParts`.

    TODO(student):
      * split on ``;`` and ``=`` into a case-insensitive dict
      * pick the deployment from ``Deployment`` / ``DeploymentId`` / ``Model``
        (raise ``ValueError`` if more than one is present)
      * pick the endpoint from ``EndpointAIInference`` / ``Endpoint``
      * capture ``Key`` (absent ⇒ managed identity) and ``ApiVersion``
    """
    raise NotImplementedError("TODO(student): implement parse_connection_string")


def resolve_model_config(
    settings: Settings | None = None,
    *,
    credential: Any | None = None,
) -> dict[str, Any]:
    """Build the judge ``AzureOpenAIModelConfiguration`` from settings.

    TODO(student):
      * default ``settings`` from ``agent_evals_workshop.config.get_settings()``
      * raise a clear ``RuntimeError`` when ``settings.is_configured`` is False
      * default ``credential`` to ``azure.identity.DefaultAzureCredential()``
      * return ``azure.ai.evaluation.AzureOpenAIModelConfiguration(...)`` with
        ``azure_endpoint``, ``azure_deployment=settings.chat_deployment_name``
        (``"chat"``), ``api_version`` and ``credential``
    """
    raise NotImplementedError("TODO(student): implement resolve_model_config")


__all__ = [
    "FoundryConnectionParts",
    "parse_connection_string",
    "resolve_model_config",
]
