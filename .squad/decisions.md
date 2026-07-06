# Squad Decisions

## Active Decisions

### 2026-07-06: Migrated project constitution from .specify (v1.0.0)

Source: `.specify/memory/constitution.md` (Ratified 2025-12-09). These are binding
principles for all work in this repo — reviewers (Skinner, Scully) block changes that
violate them.

**Core Principles:**
1. **Learning-First, Demo-Ready** — Every artifact must run in a workshop setting within
   minutes, with clear walk-throughs and minimal prerequisites. Keep friction low.
2. **Tests Define Agent Behavior (AI Evals SDK)** — Agent behavior is specified through
   deterministic tests/evals BEFORE implementation. Pin inputs, seeds, and scoring
   baselines; mock or record external calls; failing tests precede new behavior.
3. **Stable Contracts & Safe Isolation** — Agents expose explicit typed contracts and
   schema-stable prompts, no hidden state. Local runs work without live cloud secrets by
   default; Azure Foundry calls are stubbed, recorded, or gated behind opt-in config.
4. **Traceable Evaluations & Observability** — Eval runs emit structured logs (inputs,
   outputs, scores, seeds, SDK diagnostics) and retain sample transcripts. No PII in
   fixtures; real data is synthetic or sanitized.
5. **Stack Alignment & Minimalism** — Primary stack: .NET 10 / C#, Agent Framework,
   Microsoft AI Evals SDK, Azure Foundry (deployment examples). Prefer the smallest added
   dependencies; document any new runtime requirement with offline-friendly defaults.

**Tech guardrails:** Target .NET 10 + C# with Agent Framework; pin AI Evals SDK versions
with sample configs; Azure Foundry stays optional/opt-in (env templates, no committed
secrets, local mocks/recordings); fixtures stay synthetic; generated assets stay small.

**Quality gates:** Define agent contract + eval cases + expected scoring in specs/plans
before implementation; every change ships with or preserves passing AI Evals SDK tests;
add structured logging around eval flows (explain new log fields in PRs); docs accompany
feature changes; CI runs the eval suite and local scripts mirror CI.

**Governance:** This constitution is supreme; conflicts resolve in its favor. Amendments
require PRs describing governance impact + version/date bump. Semantic versioning applies.

## 2026-07-06: Python Track Vertical Slice (US0+US1) — Complete

**Reference:** skinner-python-slice-blueprint.md (full blueprint in decisions/inbox — see archive)

**Key Decisions:**

1. **Layout & Stack:** src-python/ (uv project, agent-framework 1.10/1.0.0b0 A2A, azure-ai-evaluation 1.17.0), tests-python/ (pytest skeleton + foundry marker auto-skip), solutions-python/ (full reference), dedicated AgentEvalsWorkshop.Python.AppHost (Aspire.Hosting.Python 13.4.6, AddUvicornApp().WithUv(), /health + A2A /us1agent).

2. **Agent Contract (FROZEN):** build_us1_agent(chat_client=None), get_weather_forecast() → 5-day synthetic data, get_tool_definitions() → OpenAI schema; offline-safe; ASGI entry agent_evals_workshop.app:app (FastAPI uvicorn).

3. **Implementation:** agent-framework.Agent (v1.10 class renamed, no ChatAgent alias); OpenAIChatClient (Azure via azure_endpoint+credential, AzureOpenAIChatClient deprecated); A2A hosting via A2AExecutor + DefaultRequestHandler + add_a2a_routes_to_fastapi pattern (no agent_framework.to_a2a helper exists).

4. **Evaluation Gate:** TaskAdherenceEvaluator (threshold ≥4, judge="chat"); result key task_adherence; result_key task_adherence_result must not be "fail"; portal reporting via evaluate(azure_ai_project=...) → studio_url → Foundry Evaluations tab (replaces .NET aieval HTML).

5. **Docs:** US0/US1 exercises made dual-language (🟦 .NET + 🐍 Python); DB-resource mentions removed (Postgres/pgvector no longer in scope; retrieval content retained for US2+); Next Steps link fixed (US1-retrieval-tool.md → US1-taskadheranceeval.md).

6. **Infrastructure:** Removed dangling Aspire.Hosting.Azure.PostgreSQL from existing AppHost; added AgentEvalsWorkshop.Python.AppHost to slnx; both .NET AppHosts build 0 errors.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
- The `.specify` constitution above is the authoritative source of project principles
