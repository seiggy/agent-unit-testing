# Project Context

- **Owner:** Zack Way
- **Project:** agent-unit-testing — hands-on workshop teaching how to unit test & evaluate AI agents
- **Stack:** .NET 10 / C#, .NET Aspire, Microsoft.Extensions.AI, Microsoft AI Evals SDK, Azure AI Foundry
- **Created:** 2026-07-06

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-07-06: Constitution (migrated from `.specify`) keeps Azure Foundry OPT-IN: provide environment templates, never commit secrets, supply local mocks/recordings. Local runs must work without live cloud secrets by default.
- 2026-07-06: Solution uses .NET Aspire for distributed orchestration (`src/AgentEvalsWorkshop.AppHost`, `.ServiceDefaults`). Infra lives in `infra/scripts` and `infra/seed`; keep generated/seed assets small and clearly labeled.
- 2026-07-06: CI (when present) must run the eval suite; local scripts should mirror CI commands to keep workshop parity.

## 2026-07-06: Python AppHost Created — Aspire.Hosting.Python 13.4.6 Integration

**Session:** Python track vertical slice (US0+US1), Krycek DevOps delivery.

**What:** Created dedicated AgentEvalsWorkshop.Python.AppHost (Aspire 13.4.6, net10.0) per Skinner blueprint § 4. New project: src/AgentEvalsWorkshop.Python.AppHost/AgentEvalsWorkshop.Python.AppHost.csproj (AspireHostName=agent-evals-python-host). Program.cs: AddAzureAIFoundry("az-foundry").AsExisting(...); AddUvicornApp("py-agent","../../src-python","agent_evals_workshop.app:app").WithUv().WithReference(foundry).WithEnvironment("CHAT_DEPLOYMENT_NAME","chat").WithEnvironment("AZURE_OPENAI_DEPLOYMENT_NAME",gptDeployment).WithEnvironment("AZURE_AI_PROJECT_ENDPOINT",projectEndpoint).WithHttpEndpoint(port:8111,env:"PORT").WithHttpHealthCheck("/health"). Parameters: resource-group, az-foundry-name, gpt-deployment-name, foundry-project-endpoint.

**Infrastructure Cleanup:**
- Removed Aspire.Hosting.Azure.PostgreSQL from src/AgentEvalsWorkshop.AppHost/csproj (dangling reference; Program.cs had zero Postgres usage).
- Removed Aspire.Hosting.Azure.PostgreSQL version from Directory.Packages.props.
- Added Aspire.Hosting.Python 13.4.6 version to Directory.Packages.props.
- Added AgentEvalsWorkshop.Python.AppHost to AgentEvalsWorkshop.slnx (/src/ folder).

**Aspire.Hosting.Python API Verified:**
- Package: Aspire.Hosting.Python 13.4.6 (matches Aspire line across repo).
- Method: AddUvicornApp(name, appDirectory, app) — parameter names are appDirectory + app (NOT projectDirectory/appName).
- .WithUv(): runs uv sync before app startup.
- No AddUvApp method exists.

**Build Results (Release):**
- AgentEvalsWorkshop.Python.AppHost: ✅ Build succeeded, 0 errors, 0 warnings.
- AgentEvalsWorkshop.AppHost: ✅ Build succeeded, 0 errors (pre-existing CS0168 warning unrelated to Postgres removal).

**Mulder Contract Alignment (Frozen):**
- ✅ Entry point: agent_evals_workshop.app:app
- ✅ Health check: /health → 200
- ✅ Port env: PORT (honored via WithHttpEndpoint(port:8111, env:"PORT"))
- ✅ CHAT_DEPLOYMENT_NAME=chat env var
- ✅ AZURE_AI_PROJECT_ENDPOINT passed as parameter
- ✅ src-python/ directory path recognized by AddUvicornApp (no build-time compilation required)

**Student Run Command:**
`dotnet run --project src/AgentEvalsWorkshop.Python.AppHost`

**Status:** ✅ COMPLETE. AppHost builds; Postgres cleanup complete; frozen contract honored; Mulder target (agent_evals_workshop.app:app) verified.
