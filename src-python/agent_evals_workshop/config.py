"""Configuration for the Python agent app.

Reads the Aspire-injected / ``.env`` environment contract and builds the
Azure OpenAI chat client used by the US1 agent.

Environment contract (see ``.env.example``):

===========================  =============================================
Variable                     Meaning
===========================  =============================================
``AZURE_OPENAI_ENDPOINT``    Foundry / Azure OpenAI endpoint for ``chat``
``CHAT_DEPLOYMENT_NAME``     Model deployment name (default ``chat``)
``AZURE_OPENAI_API_VERSION`` Azure OpenAI REST API version
``AZURE_AI_PROJECT_ENDPOINT``Foundry project endpoint (evaluate upload)
``PORT``                     uvicorn port (default ``8111``)
===========================  =============================================

Offline/lab-friendly: this module imports with no cloud config present.
``create_chat_client()`` only fails (with a clear message) when actually
invoked without ``AZURE_OPENAI_ENDPOINT``.
"""

from __future__ import annotations

import os
from dataclasses import dataclass

from dotenv import load_dotenv

# Load a git-ignored local `.env` if present (no-op when absent / in Aspire).
load_dotenv()

DEFAULT_CHAT_DEPLOYMENT = "chat"
DEFAULT_API_VERSION = "2025-04-01-preview"
DEFAULT_PORT = 8111


@dataclass(frozen=True)
class Settings:
    """Typed view over the environment contract."""

    azure_openai_endpoint: str | None
    chat_deployment_name: str
    azure_openai_api_version: str
    azure_ai_project_endpoint: str | None
    port: int

    @property
    def is_configured(self) -> bool:
        """True when a live Azure OpenAI endpoint is available."""
        return bool(self.azure_openai_endpoint)

    @classmethod
    def from_env(cls) -> "Settings":
        """Build settings from the current process environment."""
        port_raw = os.getenv("PORT", str(DEFAULT_PORT))
        try:
            port = int(port_raw)
        except (TypeError, ValueError):
            port = DEFAULT_PORT

        return cls(
            azure_openai_endpoint=_clean(os.getenv("AZURE_OPENAI_ENDPOINT")),
            chat_deployment_name=_clean(os.getenv("CHAT_DEPLOYMENT_NAME"))
            or DEFAULT_CHAT_DEPLOYMENT,
            azure_openai_api_version=_clean(os.getenv("AZURE_OPENAI_API_VERSION"))
            or DEFAULT_API_VERSION,
            azure_ai_project_endpoint=_clean(os.getenv("AZURE_AI_PROJECT_ENDPOINT")),
            port=port,
        )


def _clean(value: str | None) -> str | None:
    """Trim whitespace; treat empty strings as unset."""
    if value is None:
        return None
    stripped = value.strip()
    return stripped or None


def get_settings() -> Settings:
    """Return settings read from the current environment."""
    return Settings.from_env()


def create_chat_client(settings: Settings | None = None):
    """Create an Azure OpenAI chat client for the ``chat`` deployment.

    Uses :class:`agent_framework.openai.OpenAIChatClient` (Azure mode) with
    ``DefaultAzureCredential`` — no API keys. Raises ``RuntimeError`` with a
    clear message when ``AZURE_OPENAI_ENDPOINT`` is not configured, so offline
    imports never fail while live use gives an actionable error.
    """
    settings = settings or get_settings()

    if not settings.is_configured:
        raise RuntimeError(
            "AZURE_OPENAI_ENDPOINT is not set. Configure it in src-python/.env "
            "(copy from .env.example) or run under the Aspire AppHost. Auth uses "
            "DefaultAzureCredential, so also run `az login`."
        )

    # Imported lazily so the module stays importable offline and cheap to load.
    from agent_framework.openai import OpenAIChatClient
    from azure.identity.aio import DefaultAzureCredential

    return OpenAIChatClient(
        model=settings.chat_deployment_name,
        azure_endpoint=settings.azure_openai_endpoint,
        api_version=settings.azure_openai_api_version,
        credential=DefaultAzureCredential(),
    )
