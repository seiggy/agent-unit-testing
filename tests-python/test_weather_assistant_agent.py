"""US1 weather-assistant task-adherence eval (SKELETON — this is the exercise).

Python-idiomatic equivalent of ``tests/AgentEvalsWorkshop.Tests/WeatherAssistantAgentTests.cs``
(the near-empty skeleton). Write a task-adherence evaluation for the US1
``Assistant`` agent following the Arrange / Act / Assert / Report shape below.

Full reference: ``solutions-python/test_weather_assistant_agent.py``.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config
from agent_evals_workshop.agents.us1_agent import build_us1_agent, get_tool_definitions

# TaskAdherenceEvaluator scores 1–5. "Good"/"Exceptional" ≈ score >= 4.
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
    """TODO(student): implement the US1 task-adherence eval.

    Arrange:
      * ``agent = build_us1_agent(chat_client)``
      * ``query = "What's the weather like today?"``

    Act:
      * ``response = await agent.run(query)``
      * adapt it with ``helpers.agent_run.to_eval_inputs(response, query=query,``
        ``tool_definitions=get_tool_definitions())``

    Assert (local gate):
      * ``from azure.ai.evaluation import TaskAdherenceEvaluator``
      * ``score = TaskAdherenceEvaluator(model_config)(query=..., response=...,``
        ``tool_definitions=...)``
      * ``assert score["task_adherence_result"] != "fail"``
      * ``assert score["task_adherence"] >= GOOD_ADHERENCE_THRESHOLD``

    Report (Azure AI Foundry portal):
      * write a JSONL row with ``helpers.agent_run.write_jsonl``
      * ``from azure.ai.evaluation import evaluate``
      * ``result = evaluate(data=..., evaluators={"task_adherence": ...},``
        ``azure_ai_project=settings.azure_ai_project_endpoint, output_path=...)``
      * ``print("Foundry Studio:", result.get("studio_url"))``

    Delete the skip below once you start implementing.
    """
    pytest.skip("TODO(student): implement test_agent_retrieves_weather")
