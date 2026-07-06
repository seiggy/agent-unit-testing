"""Foundry / Azure OpenAI config resolver (SOLUTION reference).

Python-idiomatic equivalent of ``FoundryConnectionStringParser.cs``
(``FoundryConnectionStringParts``). Two responsibilities:

* :func:`parse_connection_string` — parse an Aspire ``Key=Value;...`` connection
  string (``Endpoint=...;Deployment=...;Key=...``) into typed parts, mirroring
  the .NET ``DbConnectionStringBuilder`` parsing.
* :func:`resolve_model_config` — build the **judge** ``AzureOpenAIModelConfiguration``
  used by ``azure-ai-evaluation`` evaluators from the app :class:`~agent_evals_workshop.config.Settings`.

The judge deployment is the lab's ``chat`` deployment (same model the agent uses)
per the workshop decision. Auth is keyless via ``DefaultAzureCredential``.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from agent_evals_workshop import config
from agent_evals_workshop.config import Settings

# Keys recognised in an Aspire/Foundry connection string (case-insensitive),
# matching the .NET FoundryConnectionStringParser behaviour.
_DEPLOYMENT_KEYS = ("deployment", "deploymentid", "model")
_ENDPOINT_KEYS = ("endpointaiinference", "endpoint")


@dataclass(frozen=True)
class FoundryConnectionParts:
    """Typed view over a parsed Foundry connection string."""

    endpoint: str | None = None
    deployment: str | None = None
    api_key: str | None = None
    api_version: str | None = None

    @property
    def uses_managed_identity(self) -> bool:
        """True when no key is present (keyless / ``DefaultAzureCredential``)."""
        return self.api_key is None


def parse_connection_string(connection_string: str | None) -> FoundryConnectionParts:
    """Parse a ``Key=Value;...`` connection string into :class:`FoundryConnectionParts`.

    Mirrors ``FoundryConnectionStringParts.ParseConnectionString``: understands
    ``Deployment`` / ``DeploymentId`` / ``Model`` (only one allowed), ``Endpoint`` /
    ``EndpointAIInference``, ``Key`` and ``ApiVersion``. Absent key ⇒ managed identity.
    """
    if not connection_string:
        return FoundryConnectionParts()

    pairs: dict[str, str] = {}
    for segment in connection_string.split(";"):
        segment = segment.strip()
        if not segment or "=" not in segment:
            continue
        key, _, value = segment.partition("=")
        pairs[key.strip().lower()] = value.strip()

    present_deployment_keys = [k for k in _DEPLOYMENT_KEYS if k in pairs]
    if len(present_deployment_keys) > 1:
        raise ValueError(
            "Connection string cannot contain more than one of "
            "'Deployment', 'DeploymentId', or 'Model'."
        )

    deployment = next((pairs[k] for k in _DEPLOYMENT_KEYS if k in pairs), None)
    endpoint = next((pairs[k] for k in _ENDPOINT_KEYS if k in pairs), None)

    return FoundryConnectionParts(
        endpoint=endpoint,
        deployment=deployment,
        api_key=pairs.get("key"),
        api_version=pairs.get("apiversion"),
    )


def resolve_model_config(
    settings: Settings | None = None,
    *,
    credential: Any | None = None,
) -> dict[str, Any]:
    """Build the judge ``AzureOpenAIModelConfiguration`` from settings.

    Returns an ``azure.ai.evaluation.AzureOpenAIModelConfiguration`` (a ``TypedDict``)
    pointing at the ``chat`` deployment. When no ``credential`` is supplied a keyless
    ``azure.identity.DefaultAzureCredential`` is used (run ``az login``).

    Raises ``RuntimeError`` when ``AZURE_OPENAI_ENDPOINT`` is unset so callers fail
    with an actionable message rather than a confusing auth error later.
    """
    settings = settings or config.get_settings()
    if not settings.is_configured:
        raise RuntimeError(
            "AZURE_OPENAI_ENDPOINT is not set — cannot build a judge model_config. "
            "Configure src-python/.env (copy .env.example) or run under the Aspire "
            "AppHost, then `az login`."
        )

    from azure.ai.evaluation import AzureOpenAIModelConfiguration

    if credential is None:
        from azure.identity import DefaultAzureCredential

        credential = DefaultAzureCredential()

    return AzureOpenAIModelConfiguration(
        azure_endpoint=settings.azure_openai_endpoint,
        azure_deployment=settings.chat_deployment_name,
        api_version=settings.azure_openai_api_version,
        credential=credential,
    )


__all__ = [
    "FoundryConnectionParts",
    "parse_connection_string",
    "resolve_model_config",
]
