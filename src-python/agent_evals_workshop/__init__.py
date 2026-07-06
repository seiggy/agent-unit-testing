"""Agent Evals Workshop — Python track package.

Exposes the US1 weather-assistant agent (behavior parity with the .NET
``US1Agent``) plus configuration helpers. The FastAPI ASGI app lives in
:mod:`agent_evals_workshop.app`.
"""

from .config import Settings, create_chat_client, get_settings

__all__ = ["Settings", "create_chat_client", "get_settings"]
