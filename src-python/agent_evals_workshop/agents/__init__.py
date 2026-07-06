"""US1 weather-assistant agent (public contract).

Behavior parity with ``src/AgentEvalsWorkshop/Agents/US1Agent.cs``:
a US-based personal assistant that uses imperial units and exposes a
5-day weather-forecast tool.

The **public API** below is the frozen contract other specialists build
against (Krycek/Scully/Byers):

* ``get_weather_forecast() -> list[dict]``
* ``build_us1_agent(chat_client=None)``
* ``get_tool_definitions() -> list[dict]``
"""
