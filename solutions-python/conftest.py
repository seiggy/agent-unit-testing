"""Pytest fixtures for the Agent Evals Workshop — Python track (SOLUTION reference).

Python-idiomatic equivalent of ``BaseIntegrationTest.cs``: instead of a base
class + ``[AssemblyInitialize]`` it uses pytest fixtures and a collection hook.

Offline-safe: any test marked ``@pytest.mark.foundry`` is auto-skipped when
``AZURE_OPENAI_ENDPOINT`` is unset, so ``pytest --collect-only`` and the offline
smoke run never touch Azure.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config

# Make ``helpers`` importable regardless of the working directory pytest is
# launched from (the tests dir is added to sys.path by pytest's rootdir import).
from helpers.foundry_config import resolve_model_config  # noqa: F401  (re-exported for tests)


def pytest_configure(config: pytest.Config) -> None:
    """Register the ``foundry`` marker even when the src-python pyproject config
    isn't the active rootdir config (e.g. ``pytest ../solutions-python``)."""
    config.addinivalue_line(
        "markers",
        "foundry: requires a live Foundry chat deployment (skipped offline)",
    )


def pytest_runtest_setup(item: pytest.Item) -> None:
    """Auto-skip ``@pytest.mark.foundry`` tests when no live endpoint is set.

    Parity with the .NET rule "skip integration tests without a live Foundry
    chat deployment". Runs before fixture setup, so the (live-only) fixtures
    below are never invoked offline.
    """
    if item.get_closest_marker("foundry") is not None:
        if not config.get_settings().is_configured:
            pytest.skip(
                "foundry: AZURE_OPENAI_ENDPOINT not set — skipping live Foundry test "
                "(offline). Configure src-python/.env and run `az login` to enable."
            )


@pytest.fixture(scope="session")
def settings() -> config.Settings:
    """Typed app settings; skip the session when unconfigured (offline)."""
    resolved = config.get_settings()
    if not resolved.is_configured:
        pytest.skip("AZURE_OPENAI_ENDPOINT not set — no live configuration.")
    return resolved


@pytest.fixture
def chat_client(settings: config.Settings):
    """Azure OpenAI chat client for the ``chat`` deployment (keyless auth)."""
    return config.create_chat_client(settings)


@pytest.fixture
def model_config(settings: config.Settings) -> dict:
    """Judge ``AzureOpenAIModelConfiguration`` for the AI-assisted evaluators.

    Uses the same ``chat`` deployment as the agent (workshop decision) and keyless
    ``DefaultAzureCredential`` auth.
    """
    return resolve_model_config(settings)


@pytest.fixture
def project_client(settings: config.Settings):
    """``AIProjectClient`` for the Foundry project (portal upload / datasets).

    Skips when ``AZURE_AI_PROJECT_ENDPOINT`` is unset so the local evaluation gate
    can still run without a project.
    """
    if not settings.azure_ai_project_endpoint:
        pytest.skip("AZURE_AI_PROJECT_ENDPOINT not set — Foundry portal upload disabled.")

    from azure.ai.projects import AIProjectClient
    from azure.identity import DefaultAzureCredential

    client = AIProjectClient(
        endpoint=settings.azure_ai_project_endpoint,
        credential=DefaultAzureCredential(),
    )
    try:
        yield client
    finally:
        client.close()


@pytest.fixture
def scenario_name(request: pytest.FixtureRequest) -> str:
    """``"<module>::<test>"`` label — parity with the .NET ``ScenarioName``."""
    return f"{request.node.module.__name__}::{request.node.name}"
