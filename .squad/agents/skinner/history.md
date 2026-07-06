# Project Context

- **Owner:** Zack Way
- **Project:** agent-unit-testing — hands-on workshop teaching how to unit test & evaluate AI agents (retrieval accuracy, tool calling, task adherence, intent resolution, prompt iteration)
- **Stack:** .NET 10 / C#, .NET Aspire, Microsoft.Extensions.AI, Microsoft AI Evals SDK, Azure AI Foundry
- **Created:** 2026-07-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-07-06: Project constitution migrated from `.specify/memory/constitution.md` (v1.0.0) into `.squad/decisions.md`. Five core principles govern all work: Learning-First/Demo-Ready, Tests-Define-Behavior (AI Evals SDK), Stable-Contracts/Safe-Isolation, Traceable-Evaluations/Observability, Stack-Alignment/Minimalism. As Lead I block changes that violate these.
- 2026-07-06: Quality gate — agent contract + eval cases + expected scoring must be defined in specs/plans before implementation. Every change ships with or preserves passing AI Evals SDK tests.
- 2026-07-06: Solution is `AgentEvalsWorkshop.slnx` with Aspire projects (App, AppHost, ServiceDefaults) under `src/`, tests under `tests/AgentEvalsWorkshop.Tests`, plus `exercises/`, `solutions/`, `infra/`.

## 2026-07-06: Python Track Vertical Slice (US0+US1) — Blueprint Decomposed

**Session:** Python track vertical slice (US0+US1), Scribe orchestration.

**What:** Authored blueprint for Python learning track parallel to .NET; specified frozen agent contract (build_us1_agent, get_weather_forecast, get_tool_definitions); eval gate (TaskAdherenceEvaluator, threshold ≥4, judge=chat); stack (uv, agent-framework 1.10/1.0.0b0 A2A, azure-ai-evaluation 1.17.0, pytest); infrastructure (AgentEvalsWorkshop.Python.AppHost, Aspire.Hosting.Python 13.4.6, AddUvicornApp().WithUv()); documentation (US0/US1 dual-language exercises, DB-resource removal, Foundry-portal reporting, Next Steps link fix).

**Key Decisions:**
1. Frozen contract honored throughout delivery (no contract changes by mulder/krycek/scully/byers).
2. Eval gate: TaskAdherenceEvaluator result key `task_adherence`, threshold ≥4, judge="chat" (deterministic per Constitution #2).
3. Lean agent-framework sub-packages (agent-framework-core/openai/a2a) reduce Windows build fragility vs. meta-package.
4. Offline-first design: /health=200 always; /us1agent=503 when AZURE_OPENAI_ENDPOINT unset (Constitution #3 stable-isolation).
5. Foundry-portal reporting (evaluate(..., azure_ai_project=...) → studio_url → Evaluations tab) replaces .NET HTML reports (Constitution #4 observability).
6. DB-resource removal (Postgres/pgvector) from US0+US1 scope; retrieval content retained for US2+.

**Status:** ✅ COMPLETE. Team consensus achieved; all 5 agents executed scope; blueprint archived in decisions.md with concise summary + pointer; orchestration logs created per agent.

**Verified Against Constitution:**
- #1 Learning-First: Dual-language exercises, minimal deps, uv + FastAPI local.
- #2 Tests Define Behavior: TaskAdherenceEvaluator deterministic gate, foundry marker auto-skip offline, fixtures specified.
- #3 Stable Contracts & Safe Isolation: Frozen contract verified unchanged; offline safety pattern; config isolation (env vars).
- #4 Traceable Evaluations: Foundry-portal reporting, structured metrics (task_adherence), synthetic data (no PII).
- #5 Stack Alignment & Minimalism: .NET 10 + Python (uv); minimal agent-framework sub-packages; azure-ai-evaluation 1.17.0; Aspire 13.4.6.
