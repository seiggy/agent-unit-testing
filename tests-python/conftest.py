"""Pytest fixtures for the Agent Evals Workshop — Python track (SKELETON).

Python-idiomatic equivalent of ``BaseIntegrationTest.cs``: pytest fixtures +
a collection hook instead of a base class + ``[AssemblyInitialize]``.

The offline-safety hook (``pytest_runtest_setup``) is provided complete — it
auto-skips ``@pytest.mark.foundry`` tests when ``AZURE_OPENAI_ENDPOINT`` is unset.
**You implement the fixture bodies below.** The full reference lives in
``solutions-python/conftest.py``.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config


def pytest_configure(config: pytest.Config) -> None:
    """Register the ``foundry`` marker even when the src-python pyproject config
    isn't the active rootdir config (e.g. ``pytest ../tests-python`` from src-python)."""
    config.addinivalue_line(
        "markers",
        "foundry: requires a live Foundry chat deployment (skipped offline)",
    )


def pytest_runtest_setup(item: pytest.Item) -> None:
    """Auto-skip ``@pytest.mark.foundry`` tests when no live endpoint is set.

    Provided complete — runs before fixture setup, so the live-only fixtures
    below are never invoked offline. (Parity with the .NET "skip without a live
    Foundry chat deployment" rule.)
    """
    if item.get_closest_marker("foundry") is not None:
        if not config.get_settings().is_configured:
            pytest.skip(
                "foundry: AZURE_OPENAI_ENDPOINT not set — skipping live Foundry test "
                "(offline). Configure src-python/.env and run `az login` to enable."
            )


@pytest.fixture(scope="session")
def settings() -> config.Settings:
    """Typed app settings; skip the session when unconfigured (offline).

    TODO(student):
      * ``resolved = config.get_settings()``
      * ``pytest.skip(...)`` when ``not resolved.is_configured``
      * else ``return resolved``
    """
    raise NotImplementedError("TODO(student): implement the settings fixture")


@pytest.fixture
def chat_client(settings: config.Settings):
    """Azure OpenAI chat client for the ``chat`` deployment (keyless auth).

    TODO(student): return ``config.create_chat_client(settings)``.
    """
    raise NotImplementedError("TODO(student): implement the chat_client fixture")


@pytest.fixture
def model_config(settings: config.Settings) -> dict:
    """Judge ``AzureOpenAIModelConfiguration`` for the AI-assisted evaluators.

    TODO(student): return ``helpers.foundry_config.resolve_model_config(settings)``
    (judge deployment = ``chat``, keyless ``DefaultAzureCredential``).
    """
    raise NotImplementedError("TODO(student): implement the model_config fixture")


@pytest.fixture
def project_client(settings: config.Settings):
    """``AIProjectClient`` for the Foundry project (portal upload / datasets).

    TODO(student):
      * ``pytest.skip(...)`` when ``settings.azure_ai_project_endpoint`` is unset
      * build ``AIProjectClient(endpoint=..., credential=DefaultAzureCredential())``
      * ``yield`` it and ``close()`` in a ``finally``
    """
    raise NotImplementedError("TODO(student): implement the project_client fixture")


@pytest.fixture
def scenario_name(request: pytest.FixtureRequest) -> str:
    """``"<module>::<test>"`` label — parity with the .NET ``ScenarioName``.

    TODO(student): return ``f"{request.node.module.__name__}::{request.node.name}"``.
    """
    raise NotImplementedError("TODO(student): implement the scenario_name fixture")
