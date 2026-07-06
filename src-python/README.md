# Agent Evals Workshop — Python Track

Python implementation of the workshop's **US1 weather-assistant agent**, with
behavior parity to the .NET `US1Agent`. Built with the
[Microsoft Agent Framework](https://github.com/microsoft/agent-framework) and
served over **A2A** (agent-to-agent JSON-RPC) plus a FastAPI `/health` endpoint.

## Prerequisites

- [**uv**](https://docs.astral.sh/uv/) (`pip install uv` or the standalone installer)
- **Python 3.12** (uv installs it automatically if missing)
- **Azure CLI** — `az login` (auth uses `DefaultAzureCredential`, no API keys)

## Quickstart

```powershell
# From src-python/
uv sync                      # create .venv + install deps (incl. dev group)

# Configure (copy the template, then edit .env — it is git-ignored)
Copy-Item .env.example .env

# Confirm the package imports cleanly (works offline, no cloud config needed)
uv run python -c "import agent_evals_workshop.app; print('ok')"

# Run the agent service (A2A at /us1agent + /health)
uv run uvicorn agent_evals_workshop.app:app --port 8111
```

Health check: `GET http://localhost:8111/health` → `200 {"status": "ok"}`.
A2A agent card: `GET http://localhost:8111/us1agent/.well-known/agent-card.json`.

## Environment contract

Injected by the Aspire AppHost or read from a local git-ignored `.env`
(see `.env.example`):

| Variable                     | Meaning                                             |
| ---------------------------- | --------------------------------------------------- |
| `AZURE_OPENAI_ENDPOINT`      | Foundry / Azure OpenAI endpoint for the `chat` model |
| `CHAT_DEPLOYMENT_NAME`       | Deployment name (default `chat`)                    |
| `AZURE_OPENAI_API_VERSION`   | Azure OpenAI REST API version                       |
| `AZURE_AI_PROJECT_ENDPOINT`  | Foundry project endpoint (`evaluate()` upload)      |
| `PORT`                       | uvicorn port (default `8111`)                       |

## Offline / lab-friendly behavior

- The package **always imports** with no cloud config present.
- `get_weather_forecast()` returns **deterministic synthetic** data (no RNG,
  no network) so tests are stable offline.
- If `AZURE_OPENAI_ENDPOINT` is unset, `/health` still returns `200`, but the
  `/us1agent` A2A route returns `503` (the live chat client is unavailable).
  `create_chat_client()` raises a clear, actionable error when invoked.

## Public API (frozen contract)

`agent_evals_workshop.agents.us1_agent`:

- `get_weather_forecast() -> list[dict]` — 5-day imperial forecast
  (`{date, temperature_f, summary}`).
- `build_us1_agent(chat_client=None)` — the `Assistant` agent; defaults the
  client via `config.create_chat_client()`.
- `get_tool_definitions() -> list[dict]` — JSON-schema tool defs for the evaluator.

`agent_evals_workshop.config`: `Settings`, `get_settings()`, `create_chat_client()`.

ASGI target (frozen): `agent_evals_workshop.app:app`.

## Tests

Test scaffolding lives in `../tests-python/` (owned by the test track). Run:

```powershell
uv run pytest
```

`foundry`-marked tests skip automatically when `AZURE_OPENAI_ENDPOINT` is unset.
