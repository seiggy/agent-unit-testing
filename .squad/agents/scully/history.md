# Project Context

- **Owner:** Zack Way
- **Project:** agent-unit-testing — hands-on workshop teaching how to unit test & evaluate AI agents
- **Stack:** .NET 10 / C#, .NET Aspire, Microsoft.Extensions.AI, Microsoft AI Evals SDK, Azure AI Foundry
- **Created:** 2026-07-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-07-06: Constitution principle #2 (migrated from `.specify`) makes me central: agent behavior is specified through deterministic tests/evals BEFORE implementation, with pinned inputs, seeds, and scoring baselines. Failing tests must precede new behavior.
- 2026-07-06: Eval runs must emit structured logs (inputs, outputs, scores, seeds, SDK diagnostics) and retain sample transcripts. Fixtures stay synthetic — no PII, no real user data, no committed secrets.
- 2026-07-06: Test project is `tests/AgentEvalsWorkshop.Tests`. Workshop eval dimensions: retrieval accuracy, tool calling, task adherence, intent resolution, prompt iteration (meta-prompt loops). CI must run the eval suite; local scripts should mirror CI.

## 2026-07-06: Python Pytest Scaffolding (US0+US1) — Tests + Solutions Delivered

**Session:** Python track vertical slice (US0+US1), Scully test/evals delivery.

**What:** Delivered Python-track pytest scaffolding per Skinner blueprint § 5. Skeleton (student implementation): tests-python/ (conftest.py, helpers/foundry_config.py, helpers/agent_run.py, test_configuration_smoke.py, test_weather_assistant_agent.py — all TODO stubs with full Arrange/Act/Assert guidance). Solution (full reference): solutions-python/ (mirror of tests-python/, fully implemented). All offline-safe with foundry marker auto-skip when AZURE_OPENAI_ENDPOINT unset.

**Azure-AI-Evaluation API Verified (v1.17.0 + azure-ai-projects 2.3.0):**
- Construct: TaskAdherenceEvaluator(model_config, *, threshold=0, credential=None).
- model_config: AzureOpenAIModelConfiguration TypedDict(azure_endpoint=..., azure_deployment="chat", api_version=..., credential=DefaultAzureCredential()).
- Judge deployment: "chat" (per user decision + Mulder CHAT_DEPLOYMENT_NAME env var).
- Call: evaluator(query=<str|list>, response=<str|list>, tool_definitions=<list[dict]>).
- **EXACT Result Keys** (prefix "task_adherence"): task_adherence (float 1-5 score), task_adherence_score, task_adherence_passed (bool), task_adherence_result ("pass"/"fail"), task_adherence_reason (str), task_adherence_status, task_adherence_threshold, task_adherence_properties.
- Good/Exceptional gate: assert result["task_adherence"] >= 4 AND result["task_adherence_result"] != "fail" (module const GOOD_ADHERENCE_THRESHOLD=4).
- Batch/portal: evaluate(*, data=<jsonl path>, evaluators={"task_adherence": ev}, azure_ai_project=<AZURE_AI_PROJECT_ENDPOINT str>, output_path=..., evaluation_name=..., tags=...) → EvaluationResult TypedDict with metrics, rows, studio_url (NotRequired), oai_eval_run_ids. Print eval_result.get("studio_url") for Foundry Evaluations tab.
- AIProjectClient(endpoint=..., credential=DefaultAzureCredential()) for project_client fixture (skips when AZURE_AI_PROJECT_ENDPOINT unset).

**Foundry-Portal Reporting (Replaces .NET aieval HTML/C:\TestReports):**
- Local callable-evaluator assertion is the deterministic gate (Constitution #2 — tests define behavior).
- evaluate(..., azure_ai_project=...) uploads run to Foundry + returns studio_url.
- Student flow: print eval_result.get("studio_url") → open in Azure AI Foundry Evaluations tab.
- No dotnet aieval, no HTML file generation.

**Agent-Run Adapter:**
- AgentResponse.text → final assistant text.
- .messages[*].contents where content.type=="function_call" exposes call_id/name/arguments.
- Verified offline against real agent_framework Message/Content objects.

**Deps:**
- Added httpx >= 0.28 to src-python/pyproject.toml [dependency-groups].dev (used by US0 smoke /health GET).
- azure-ai-projects already in main deps.
- uv sync succeeds.

**Rootdir Gotcha Mitigated:**
- Running `uv run pytest ../tests-python` from src-python/ sets rootdir=repo root (pyproject.toml marker config not loaded).
- Workaround: pytest_configure registers `foundry` marker in conftest; explicit @pytest.mark.asyncio works in STRICT mode.
- agent_evals_workshop imports via installed editable package (robust either way).

**Validation (Offline, No Creds):**
- `uv run pytest ../tests-python --collect-only` → 2 tests, no import errors, no marker warnings.
- `uv run pytest ../tests-python -k configuration_smoke` → 1 skipped (foundry offline), correct reason.
- `uv run pytest ../solutions-python` → 3 skipped offline; imports clean; TaskAdherenceEvaluator construction sanity-checked with synthetic AgentResponse.

**For Byers (Exercise Docs):**
- Run cmds from src-python/
- US0: `uv run pytest ../tests-python -k configuration_smoke`
- US1: `uv run pytest ../tests-python -k weather -m foundry`
- US1 solution to paste verbatim: solutions-python/test_weather_assistant_agent.py
- Result keys: task_adherence, task_adherence_result, task_adherence_reason; threshold 4; studio_url print.
- Replace aieval HTML step with evaluate(..., azure_ai_project=...) → Studio URL → Foundry Evaluations tab flow.

**Status:** ✅ COMPLETE. Tests/solutions delivered; azure-ai-evaluation API verified; offline safety ensured; Foundry-portal flow validated; ready for Byers doc integration + student exercise.
