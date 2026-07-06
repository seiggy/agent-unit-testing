"""US1 weather-assistant task-adherence eval (SOLUTION reference).

Python-idiomatic equivalent of ``solutions/WeatherAssistantAgentTests.cs``:

* **Arrange** — build the US1 ``Assistant`` agent (``build_us1_agent``).
* **Act** — ``await agent.run("What's the weather like today?")`` and adapt the
  result into evaluator inputs (query / response / tool_definitions).
* **Assert** — score with ``TaskAdherenceEvaluator(model_config)`` and require the
  Good/Exceptional band (numeric score ≥ 4 on the 1–5 scale), the local gate.
* **Report** — push the run to the Azure AI Foundry portal with
  ``evaluate(..., azure_ai_project=...)`` and print the returned Studio URL
  (replaces the .NET ``aieval`` HTML report / ``C:\\TestReports`` flow).

Marked ``@pytest.mark.foundry`` — auto-skipped offline by the ``conftest`` hook.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config
from agent_evals_workshop.agents.us1_agent import build_us1_agent, get_tool_definitions
from helpers.agent_run import to_eval_inputs, write_jsonl

# TaskAdherenceEvaluator scores 1–5. "Good"/"Exceptional" ≈ score >= 4
# (parity with the .NET EvaluationRating.Good / .Exceptional gate).
GOOD_ADHERENCE_THRESHOLD = 4


@pytest.mark.foundry
@pytest.mark.asyncio
async def test_agent_retrieves_weather(
    chat_client,
    model_config,
    settings: config.Settings,
    project_client,
    scenario_name,
    tmp_path,
) -> None:
    from azure.ai.evaluation import TaskAdherenceEvaluator, evaluate

    # Arrange
    agent = build_us1_agent(chat_client)
    query = "What's the weather like today?"

    # Act
    response = await agent.run(query)
    inputs = to_eval_inputs(response, query=query, tool_definitions=get_tool_definitions())

    # Assert — local, deterministic gate via the callable evaluator.
    task_adherence = TaskAdherenceEvaluator(model_config)
    score = task_adherence(
        query=inputs.query,
        response=inputs.response,
        tool_definitions=inputs.tool_definitions,
    )
    # Result-dict keys are prefixed with the evaluator result key "task_adherence".
    assert score["task_adherence_result"] != "fail", score["task_adherence_reason"]
    assert score["task_adherence"] >= GOOD_ADHERENCE_THRESHOLD, score["task_adherence_reason"]

    # Report — upload the run to the Azure AI Foundry portal (Evaluations tab).
    jsonl_path = write_jsonl(inputs, tmp_path / "us1.jsonl")
    eval_result = evaluate(
        data=str(jsonl_path),
        evaluation_name=scenario_name,
        evaluators={"task_adherence": task_adherence},
        azure_ai_project=settings.azure_ai_project_endpoint,
        output_path=str(tmp_path / "us1-eval.json"),
    )
    studio_url = eval_result.get("studio_url")
    print(f"Foundry Studio: {studio_url}")
    assert eval_result["metrics"], "evaluate() returned no aggregated metrics."
