"""US1 weather-assistant agent — frozen public API.

Behavior parity with ``src/AgentEvalsWorkshop/Agents/US1Agent.cs``.
API shape is Python-idiomatic (per blueprint §3c), not a 1:1 mirror.
"""

from __future__ import annotations

from datetime import date, timedelta
from typing import Any

# Parity with US1Agent.cs Summaries (index 0..9, coldest -> hottest).
SUMMARIES: tuple[str, ...] = (
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
)

# Instructions mirror US1Agent.cs (US-based assistant, imperial units).
US1_INSTRUCTIONS = (
    "You are a personal assistant for a user based in the United States.\n"
    "When providing information, always use the imperial measurement system "
    "(inches, feet, miles, pounds, Fahrenheit, etc.) unless explicitly "
    "instructed otherwise.\n"
    "Ensure that your responses are tailored to the cultural context of the "
    "United States.\n"
    "Your goal is to assist the user effectively while adhering to these "
    "guidelines."
)

US1_AGENT_NAME = "Assistant"

_TOOL_NAME = "get_weather_forecast"
_TOOL_DESCRIPTION = "Get a 5-day weather forecast. Returns imperial units (Fahrenheit)."


def get_weather_forecast() -> list[dict]:
    """Get a 5-day weather forecast.

    Returns 5 entries of imperial (Fahrenheit) data, one per upcoming day:
    ``{"date": "YYYY-MM-DD", "temperature_f": int, "summary": str}``.

    The data is **deterministic synthetic** — derived purely from each day's
    ordinal (no RNG, no network) so offline tests are stable and the tool is
    safe to run without any cloud config.
    """
    today = date.today()
    forecast: list[dict] = []
    for index in range(1, 6):
        day = today + timedelta(days=index)
        ordinal = day.toordinal()
        # Deterministic Fahrenheit in a plausible ~[5, 95] band.
        temperature_f = 5 + (ordinal * 37) % 91
        summary = SUMMARIES[ordinal % len(SUMMARIES)]
        forecast.append(
            {
                "date": day.isoformat(),
                "temperature_f": temperature_f,
                "summary": summary,
            }
        )
    return forecast


def get_tool_definitions() -> list[dict]:
    """Return JSON-schema tool definitions for the evaluator.

    Shape matches the OpenAI/Azure ``tool_definitions`` contract consumed by
    ``azure-ai-evaluation`` evaluators (TaskAdherence / ToolCallAccuracy).
    ``get_weather_forecast`` takes no parameters.
    """
    return [
        {
            "type": "function",
            "function": {
                "name": _TOOL_NAME,
                "description": _TOOL_DESCRIPTION,
                "parameters": {
                    "type": "object",
                    "properties": {},
                    "required": [],
                },
            },
        }
    ]


def build_us1_agent(chat_client: Any | None = None):
    """Build the US1 ``Assistant`` agent (agent_framework ``ChatAgent``).

    * instructions mirror ``US1Agent.cs`` (US-based assistant, imperial units)
    * ``tools=[get_weather_forecast]``
    * ``chat_client`` defaults to :func:`config.create_chat_client` (Azure
      OpenAI, ``model=CHAT_DEPLOYMENT_NAME``, ``DefaultAzureCredential``).

    Returns an agent_framework ``Agent``. Import of the agent framework is lazy
    so this module stays importable offline; the client factory only requires
    cloud config when defaulted.
    """
    from agent_framework import Agent

    if chat_client is None:
        from .. import config

        chat_client = config.create_chat_client()

    return Agent(
        client=chat_client,
        name=US1_AGENT_NAME,
        instructions=US1_INSTRUCTIONS,
        tools=[get_weather_forecast],
    )


__all__ = [
    "get_weather_forecast",
    "get_tool_definitions",
    "build_us1_agent",
    "US1_INSTRUCTIONS",
    "US1_AGENT_NAME",
    "SUMMARIES",
]
