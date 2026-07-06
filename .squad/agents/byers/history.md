# Project Context

- **Owner:** Zack Way
- **Project:** agent-unit-testing — hands-on workshop teaching how to unit test & evaluate AI agents
- **Stack:** .NET 10 / C#, .NET Aspire, Microsoft.Extensions.AI, Microsoft AI Evals SDK, Azure AI Foundry
- **Created:** 2026-07-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-07-06: Constitution principle #1 (migrated from `.specify`): Learning-First / Demo-Ready. Every artifact must be runnable in a workshop setting within minutes, with clear walk-throughs and minimal prerequisites. Docs must accompany feature changes.
- 2026-07-06: Repo has `exercises/` (with images), `solutions/`, and a README describing the Aspire architecture and the five eval dimensions (retrieval accuracy, tool calling, task adherence, intent resolution, prompt iteration).
- 2026-07-06: Local scripts should mirror CI so student instructions match reality. Keep prerequisites minimal and offline-friendly (no required cloud access to start).

## 2026-07-06: Dual-Language Exercises (US0+US1) + Infrastructure Cleanup

**Session:** Python track vertical slice (US0+US1), Byers documentation delivery.

**What:** Executed Byers slice (blueprint § 6 doc plan + § 7 cleanup) in main checkout; no branch switch, no commit. Made US0+US1 exercises dual-language (.NET + Python); removed DB-resource mentions (Postgres/pgvector/Docker) from README/US0/US1; fixed Next Steps link (US1-retrieval-tool.md → US1-taskadheranceeval.md).

**Labeling Convention:**
- Uniform inline bold labels: **🟦 .NET** / **🐍 Python** introduce each language path within every step.
- Renders in plain markdown; no platform-tab dependency.
- Code reveals: existing `<details><summary>💡 …</summary>` syntax preserved, track marker appended to summary (e.g., `Show Example Implementation (🟦 .NET)` / `(🐍 Python)`).
- Every original .NET block preserved; Python counterpart added beside.
- All existing platform syntax preserved (`> [!knowledge]`, `> [+hint]`, `> [!NOTE]`, `> [!alert]`, `!IMAGE[...]`, `@[…][Ref]{Powershell}`, `++kbd++`).

**Files Changed:**

1. **exercises/US0-intro.md** — split Prerequisites/Open/Start-AppHost/Credentials/Verify/Troubleshooting into 🟦/🐍 paths.
   - 🐍 Python: uv + Python 3.12 prereqs; `code src-python`; `uv sync`; `dotnet run --project src/AgentEvalsWorkshop.Python.AppHost` → `py-agent` Healthy; copy `.env.example` → `.env`; `az login`; smoke test `uv run pytest ../tests-python -k configuration_smoke`.
   - Fixed broken Next Steps link: US1-retrieval-tool.md → US1-taskadheranceeval.md.

2. **exercises/US1-taskadheranceeval.md** — added 🐍 Python parallel to every 🟦 .NET step.
   - Test class → fixtures (conftest); no ReportingConfiguration; TaskAdherenceEvaluator(model_config); Arrange/Act/Assert with build_us1_agent/get_tool_definitions/to_eval_inputs.
   - Run cmd: `uv run pytest ../tests-python -k weather -m foundry`.
   - **Reporting path replaced (Python):** Foundry-portal evaluate(data=…, evaluators={…}, azure_ai_project=…) → print studio_url → Foundry Evaluations tab (vs. .NET aieval HTML report).
   - Placeholder TODO: `<!-- TODO screenshot: Foundry Evaluations tab showing the US1 task_adherence run -->` (screenshot needed; not invented).
   - Assertions: `assert score["task_adherence_result"] != "fail", score["task_adherence_reason"]` and `assert score["task_adherence"] >= 4, …`.
   - Python "Complete Solution" reveal pastes solutions-python/test_weather_assistant_agent.py VERBATIM (byte-for-byte verified).

3. **README.md** — added 🐍 Python track note to overview + blockquote callout.
   - Project structure now lists: src-python/, tests-python/, solutions-python/, AgentEvalsWorkshop.Python.AppHost (uv, agent-framework, azure-ai-evaluation).

**DB-Resource Removal (README/US0/US1 only):**
- README line 9: "vector database" → "its knowledge source" (retained retrieval-accuracy learning objective).
- README architecture mermaid: removed `Postgres[("Azure Postgres (pgvector)")]` node + `Agent <--> Postgres` edge.
- README projects table: "Main agent service with retrieval, tools…" → "…with tools and agent logic".
- README prereqs: removed `Docker Desktop (for PostgreSQL with pgvector)`.
- README project structure: removed `infra/seed/ # Seed data for PostgreSQL` and `src/…/Retrieval/ # Vector retrieval logic`.
- README resources: removed `pgvector Extension` link (added Agent Framework + azure-ai-evaluation links).
- US0 prereqs: removed `Docker Desktop running (required for Aspire)`.
- US0 troubleshooting: removed `AppHost fails to start | Docker not running` row.

**Retrieval Content Kept (Not DB Resources):**
- Retrieval-accuracy learning objective (README § Overview).
- US2 "Retrieval Evaluation with Built-in Evaluators" section + link.
- US2 file references in project structure.
- Intro sentence: "retrieval accuracy".

**Scope Validation:**
- Out-of-slice files untouched: US2/US3, infra/**, .squad/.specify/.copilot/.github, Directory.Packages.props, AppHost csproj.
- Grepped src/, exercises/, README.md, Directory.Packages.props for `[Dd]ocker|pgvector|[Pp]ostgres` → no Docker/container resource remains in src/ (AppHost Program.cs declares no containerized resource; Docker was prereq ONLY for pgvector rationale).
- Post-edit grep for `pgvector|postgres|docker|vector database|seed data` = zero matches.

**Validation:**
- Re-read both exercises end-to-end; `<details>` blocks balanced 14/14; every step has 🟦/🐍 pair; .NET blocks intact.
- Python code pastes verified verbatim vs. solutions-python/test_weather_assistant_agent.py.

**Remaining TODO for Student/User:**
- Screenshot placeholder in US1 (Python reporting): `<!-- TODO screenshot: Foundry Evaluations tab showing the US1 task_adherence run -->` — capture a real Foundry Evaluations-tab screenshot; no image invented.

**Status:** ✅ COMPLETE. Dual-language exercises ready; DB-resource removal complete; retrieval content preserved for US2+; Next Steps link fixed; all original .NET content intact; Python content verified verbatim.
