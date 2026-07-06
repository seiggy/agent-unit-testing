"""US0 configuration smoke test (SKELETON — you implement this).

Python-idiomatic equivalent of ``ConfigurationSmokeTests.cs``: prove the lab
environment resolves and the chat client builds (and, optionally, that the
running AppHost answers ``GET /health`` with 200).

Full reference: ``solutions-python/test_configuration_smoke.py``.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config


@pytest.mark.foundry
def test_configuration_smoke(settings: config.Settings) -> None:
    """TODO(student): implement the US0 smoke test.

    Steps:
      1. assert ``settings.is_configured`` and ``settings.chat_deployment_name == "chat"``
      2. build a client with ``config.create_chat_client(settings)`` (must not raise)
      3. (optional) ``import httpx`` and assert
         ``httpx.get(f"http://localhost:{settings.port}/health").status_code == 200``
         when the AppHost is running (skip when unreachable)

    Delete the skip below once you start implementing.
    """
    pytest.skip("TODO(student): implement test_configuration_smoke")
