"""US0 configuration smoke test (SOLUTION reference).

Python-idiomatic equivalent of ``ConfigurationSmokeTests.cs``: prove the lab
environment resolves, the chat client builds, and (when the AppHost is running)
the FastAPI ``/health`` probe returns 200.

Marked ``@pytest.mark.foundry`` because it needs live config; auto-skipped
offline by the ``conftest`` hook.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config


@pytest.mark.foundry
def test_configuration_smoke(settings: config.Settings) -> None:
    """Settings resolve and a chat client can be constructed."""
    # Arrange / Act
    assert settings.is_configured, "AZURE_OPENAI_ENDPOINT must be set for live tests."
    assert settings.chat_deployment_name == "chat"

    # Building the client must not raise when configured.
    client = config.create_chat_client(settings)

    # Assert
    assert client is not None


@pytest.mark.foundry
def test_health_endpoint_ok(settings: config.Settings) -> None:
    """Optional: the running AppHost service answers ``GET /health`` with 200.

    Skips when the service isn't reachable (e.g. AppHost not started) so the
    smoke test stays useful even without the dashboard running.
    """
    import httpx

    base_url = f"http://localhost:{settings.port}"
    try:
        response = httpx.get(f"{base_url}/health", timeout=5.0)
    except httpx.HTTPError as exc:
        pytest.skip(f"AppHost not reachable at {base_url} ({exc}). Start it to run this check.")

    assert response.status_code == 200
    assert response.json().get("status") == "ok"
