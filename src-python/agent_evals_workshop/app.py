"""FastAPI ASGI app for the Python agent service.

Frozen ASGI target (Krycek's uvicorn/Aspire entrypoint):
``agent_evals_workshop.app:app``.

Surfaces:

* ``GET /health`` — always ``200`` (liveness for the Aspire health check).
* ``/us1agent`` — the US1 ``Assistant`` agent exposed over **A2A** (JSON-RPC),
  mirroring the .NET ``MapA2AJsonRpc(agent, "/us1agent")``. The agent card is
  served at ``/us1agent/.well-known/agent-card.json``.

Offline/lab-friendly: the module always imports. When ``AZURE_OPENAI_ENDPOINT``
is unset, ``/health`` still returns ``200`` but the A2A route returns ``503``
(no live chat client), and a warning is logged.
"""

from __future__ import annotations

import logging

from fastapi import FastAPI
from fastapi.responses import JSONResponse

from . import config

logger = logging.getLogger(__name__)

A2A_ROUTE = "/us1agent"
AGENT_CARD_URL = f"{A2A_ROUTE}/.well-known/agent-card.json"

app = FastAPI(
    title="Agent Evals Workshop — Python (US1 Assistant)",
    description="US-based weather assistant exposed over A2A, plus a health probe.",
    version="0.1.0",
)


@app.get("/health", tags=["health"])
async def health() -> dict:
    """Liveness probe — always 200 (independent of cloud config)."""
    return {"status": "ok"}


def _build_agent_card():
    """Build the A2A agent card describing the US1 Assistant."""
    from a2a.types import AgentCapabilities, AgentCard, AgentInterface, AgentSkill

    skill = AgentSkill(
        id="get_weather_forecast",
        name="Weather Forecast",
        description="Provides a 5-day US weather forecast in imperial units (Fahrenheit).",
        tags=["weather", "forecast", "imperial"],
        examples=["What's the weather like today?"],
    )
    return AgentCard(
        name="Assistant",
        description=(
            "A personal assistant for a US-based user that always uses the "
            "imperial measurement system."
        ),
        version="0.1.0",
        default_input_modes=["text"],
        default_output_modes=["text"],
        capabilities=AgentCapabilities(streaming=True),
        skills=[skill],
        supported_interfaces=[
            AgentInterface(url=A2A_ROUTE, protocol_binding="JSONRPC"),
        ],
    )


def _mount_a2a(fastapi_app: FastAPI) -> bool:
    """Mount the US1 agent over A2A at ``/us1agent``.

    Returns True when mounted, False when skipped (offline / no chat endpoint).
    """
    settings = config.get_settings()
    if not settings.is_configured:
        logger.warning(
            "AZURE_OPENAI_ENDPOINT is not set — A2A route %s is unavailable "
            "(returns 503). /health remains available. Configure src-python/.env "
            "or run under the Aspire AppHost to enable the agent.",
            A2A_ROUTE,
        )
        return False

    from a2a.server.request_handlers import DefaultRequestHandler
    from a2a.server.routes import (
        add_a2a_routes_to_fastapi,
        create_agent_card_routes,
        create_jsonrpc_routes,
    )
    from a2a.server.tasks import InMemoryTaskStore
    from agent_framework.a2a import A2AExecutor

    from .agents.us1_agent import build_us1_agent

    agent = build_us1_agent()
    card = _build_agent_card()

    handler = DefaultRequestHandler(
        agent_executor=A2AExecutor(agent=agent),
        task_store=InMemoryTaskStore(),
        agent_card=card,
    )

    add_a2a_routes_to_fastapi(
        fastapi_app,
        agent_card_routes=create_agent_card_routes(card, card_url=AGENT_CARD_URL),
        jsonrpc_routes=create_jsonrpc_routes(handler, rpc_url=A2A_ROUTE),
    )
    logger.info("Mounted US1 agent over A2A at %s", A2A_ROUTE)
    return True


_a2a_mounted = False
try:
    _a2a_mounted = _mount_a2a(app)
except Exception:  # noqa: BLE001 — never let wiring break the health surface
    logger.exception(
        "Failed to mount the A2A agent route %s; /health stays available.", A2A_ROUTE
    )


if not _a2a_mounted:

    @app.api_route(
        A2A_ROUTE,
        methods=["GET", "POST"],
        tags=["a2a"],
        include_in_schema=False,
    )
    async def us1agent_unavailable() -> JSONResponse:
        """Fallback when the agent isn't configured (offline lab mode)."""
        return JSONResponse(
            status_code=503,
            content={
                "error": "agent_unavailable",
                "detail": (
                    "The US1 agent requires AZURE_OPENAI_ENDPOINT (and `az login`). "
                    "Configure src-python/.env or run under the Aspire AppHost."
                ),
            },
        )
