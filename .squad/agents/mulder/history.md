# Project Context

- **Owner:** Zack Way
- **Project:** agent-unit-testing — hands-on workshop teaching how to unit test & evaluate AI agents
- **Stack:** .NET 10 / C#, .NET Aspire, Microsoft.Extensions.AI, Microsoft AI Evals SDK, Azure AI Foundry
- **Created:** 2026-07-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-07-06: Constitution (migrated from `.specify`) requires agents to expose explicit typed contracts and schema-stable prompts with no hidden state. Local runs must work WITHOUT live cloud secrets by default — Azure Foundry calls are stubbed, recorded, or gated behind opt-in config.
- 2026-07-06: Behavior is tests-first — implement only after Skinner/Scully define the agent contract, eval cases, and expected scoring baselines.
- 2026-07-06: App lives in `src/AgentEvalsWorkshop` (+ AppHost, ServiceDefaults) orchestrated by .NET Aspire. Workshop demonstrates retrieval accuracy, tool calling, task adherence, intent resolution, and prompt iteration.

## 2026-07-06: Python Slice (US0+US1) Built — FROZEN Contract Honored

**Session:** Python track vertical slice (US0+US1), Mulder backend delivery.

**What:** Delivered src-python/ uv project (agent_evals_workshop) per Skinner blueprint § 2/3, honoring FROZEN agent contract unchanged. Project structure: config.py (Settings, get_settings(), create_chat_client()), agents/us1_agent.py (build_us1_agent, get_weather_forecast, get_tool_definitions), app.py (FastAPI ASGI uvicorn). Runtime deps: agent-framework-core >= 1.10, agent-framework-openai >= 1.10, agent-framework-a2a >= 1.0.0b0 (beta), a2a-sdk[http-server] >= 1.1, azure-identity, azure-ai-projects, fastapi, uvicorn[standard], python-dotenv. Dev: pytest, pytest-asyncio, azure-ai-evaluation 1.17.0. uv sync succeeds; uv.lock committed.

**Key Implementation Decisions (Verified vs. Installed Packages):**
1. **Lean sub-packages (not meta-package):** agent-framework-core/openai/a2a chosen over `agent-framework` meta-package, which resolves to agent-framework-core[all] + drags hyperlight/bedrock/native connectors → Windows build fragility. Lean pattern verified with uv.lock inspect.
2. **Agent class (v1.10):** Top-level import is `Agent` (ChatAgent not aliased in 1.10). Constructor: Agent(client=chat_client, name=..., instructions=..., tools=[...]). No client.create_agent() factory.
3. **Chat client pattern:** OpenAIChatClient(model="chat", azure_endpoint=..., api_version=..., credential=DefaultAzureCredential()). Deprecated AzureOpenAIChatClient removed.
4. **A2A hosting:** A2AExecutor + DefaultRequestHandler + add_a2a_routes_to_fastapi pattern (no agent_framework.to_a2a helper in 1.10). Mounts JSON-RPC POST at /us1agent (mirrors .NET MapA2AJsonRpc).
5. **Offline safety:** AZURE_OPENAI_ENDPOINT unset → create_chat_client() raises clear error; /health=200 always; /us1agent=503 when creds absent (verified via FastAPI TestClient).

**Frozen Contract Verification:**
- ✅ build_us1_agent(chat_client=None) + public imports
- ✅ get_weather_forecast() → list[dict] ({date, temperature_f, summary}, 5-day synthetic, deterministic)
- ✅ get_tool_definitions() → list[dict] (OpenAI function-tool schema)
- ✅ config.py: Settings, get_settings(), create_chat_client()
- ✅ Env vars: AZURE_OPENAI_ENDPOINT, CHAT_DEPLOYMENT_NAME=chat, AZURE_OPENAI_API_VERSION, AZURE_AI_PROJECT_ENDPOINT, PORT
- ✅ ASGI entry: agent_evals_workshop.app:app
- ✅ A2A route: /us1agent + /us1agent/.well-known/agent-card.json
- ✅ /health returns 200

**For Scully/Byers:**
- Import: from agent_evals_workshop.agents.us1_agent import build_us1_agent, get_weather_forecast, get_tool_definitions
- Instructions text: us1_agent.US1_INSTRUCTIONS
- Agent name: "Assistant"
- Tool definitions: OpenAI function-tool schema (type/function/name/description/parameters)
- Live-model tests: @pytest.mark.foundry (skip when AZURE_OPENAI_ENDPOINT unset)

**For Krycek:**
- uvicorn target: agent_evals_workshop.app:app
- PORT env: honored via Settings.port
- /health endpoint: 200 OK
- A2A entry: /us1agent

**Status:** ✅ COMPLETE. Contract frozen + honored; offline behavior verified; all deps pinned + locked.
