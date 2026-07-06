# RAI Audit Trail

> Append-only evidence log. Entries are redacted — never contains raw secrets or harmful content.

<!-- Rai appends findings below -->

---

## 2026-07-06T10:06Z — Python-track Vertical Slice Review

**Reviewer:** Rai  **Requested by:** Zack Way  **State backend:** local  
**Overall verdict:** 🟢 — No critical violations found.

### Artifacts reviewed

| Artifact | Verdict | Notes |
|---|---|---|
| `src-python/.env.example` | 🟢 | All values blank placeholders; comment explicitly states no API keys |
| `src-python/.gitignore` | 🟢 | `.env` properly gitignored |
| `src-python/agent_evals_workshop/config.py` | 🟢 | DefaultAzureCredential only; offline-safe; no secrets in logs |
| `src-python/agent_evals_workshop/app.py` | 🟡 | Advisory: no rate limiting; broad exception suppression (logged) |
| `src-python/agent_evals_workshop/agents/us1_agent.py` | 🟢 | Deterministic synthetic data; no user input reaches shell/LLM directly |
| `src-python/pyproject.toml` | 🟢 | No secrets; properly structured |
| `src-python/README.md` | 🟢 | No PII, no terminology violations |
| `src/AgentEvalsWorkshop.Python.AppHost/Program.cs` | 🟢 | All config via AddParameter(); no hardcoded endpoints or keys |
| `src/AgentEvalsWorkshop.Python.AppHost/appsettings.json` | 🟢 | Logging config only |
| `src/AgentEvalsWorkshop.Python.AppHost/appsettings.Development.json` | 🟡 | Default log level "Debug" — advisory: framework may emit request bodies |
| `Directory.Packages.props` | 🟢 | Versions only; CVE pin (GHSA-v5pm-xwqc-g5wc) noted positively |

### Findings

**ADVISORY-1 (🟡)** — No rate limiting on `/us1agent` A2A endpoint  
`src-python/agent_evals_workshop/app.py` — No throttle configured; unbounded LLM calls possible.

**ADVISORY-2 (🟡)** — Debug log level in Development appsettings  
`src/AgentEvalsWorkshop.Python.AppHost/appsettings.Development.json:4` — `Default: Debug` may log full request payloads including bearer tokens in dev mode.

### Checks passed (redacted evidence)

- ✅ No committed `.env`; `src-python/` untracked (not yet committed); git ls-files clean
- ✅ No hardcoded API keys, subscription IDs, tenant IDs, or connection strings in any file
- ✅ `DefaultAzureCredential` used exclusively — no key-based auth paths
- ✅ Deterministic synthetic fixture data; zero PII in fixtures or sample data
- ✅ No `eval()`/`exec()` patterns; no shell-injection or path-traversal vectors
- ✅ Offline-safe degradation implemented (health 200 always; A2A 503 with actionable message)
- ✅ AppHost uses `AddParameter()` for all cloud config; no secrets in appsettings files
- ✅ No CORS misconfiguration (FastAPI restrictive default; no `CORSMiddleware` added)
- ✅ CVE pin Microsoft.OpenApi 2.9.0 (GHSA-v5pm-xwqc-g5wc) — positive security hygiene

