# Exercise: Introduction & Environment Setup

## Goal
Set up your development environment, clone the workshop repository, and configure Azure AI Foundry connectivity to prepare for the agent evaluation exercises.

## Learning Objectives
- Clone and open the workshop repository
- Understand the solution structure
- Configure Azure AI Foundry credentials
- Verify the Aspire AppHost starts successfully

## Prerequisites
- **Visual Studio 2026 18.0+** or **Visual Studio Code** with the C# Dev Kit extension
- **.NET 10 SDK** installed
- **Docker Desktop** running (required for Aspire)
- An **Azure subscription** with access to Azure AI Foundry
- **Git** installed and configured

<details><summary>📚 <b>Documentation Links:</b></summary>
<ul>
    <li>
        <a href="https://dotnet.microsoft.com/download/dotnet/10.0">.NET 10 SDK Download</a>
    </li>
    <li>
        <a href="https://visualstudio.microsoft.com/vs/">Visual Studio 2026</a>
    </li>
    <li>
        <a href="https://www.docker.com/products/docker-desktop/">Docker Desktop</a> (or another OCI compliant container platform)
    </li>
    <li>
        <a href="https://learn.microsoft.com/en-us/azure/ai-studio/what-is-ai-studio">Microsoft Foundry Overview</a>
    </li>
</ul>
</details>

## Clone the Repository

Choose one of the following methods to clone the workshop repository.

### Option A: Using Terminal / Command Line

1. [ ] Open a terminal or command prompt
1. [ ] Run the following commands:

```bash
git clone https://github.com/seiggy/agent-unit-testing.git
cd agent-unit-testing
```

### Option B: Using Visual Studio Code

1. [ ] Open VS Code
1. [ ] Press ++Ctrl+Shift+P++ (or ++Cmd+Shift+P++ on macOS) to open the Command Palette
1. [ ] Type **Git: Clone** and select it
1. [ ] Paste the repository URL: ++https://github.com/seiggy/agent-unit-testing++
1. [ ] Choose a local folder to clone into
1. [ ] When prompted, click **Open** to open the cloned repository

### Option C: Using Visual Studio 2026

1. [ ] Open Visual Studio 2026
1. [ ] On the start window, select **Clone a repository**
1. [ ] Enter the repository URL: ++https://github.com/seiggy/agent-unit-testing++
1. [ ] Choose a local path and click **Clone**
1. [ ] Visual Studio will automatically open the solution

## Open the Solution

1. [ ] Open the ++AgentEvalsWorkshop.sln++ solution file located in the root of the cloned repository
1. [ ] Familiarize yourself with the solution structure:

> [!NOTE]
> Solution Structure
> 
> | Project | Purpose |
> |---------|---------|
> | **AgentEvalsWorkshop** | Main application containing agent definitions |
> | **AgentEvalsWorkshop.AppHost** | .NET Aspire orchestration host |
> | **AgentEvalsWorkshop.ServiceDefaults** | Shared service configuration |
> | **AgentEvalsWorkshop.Tests** | Integration tests with AI evaluators |

## Start the Aspire AppHost

The Aspire AppHost orchestrates the application and its dependencies, including the Azure AI Foundry connection.

### Using Visual Studio 2026

1. [ ] In **Solution Explorer**, right-click on ++AgentEvalsWorkshop.AppHost++
1. [ ] Select **Set as Startup Project**
1. [ ] Press _F5_ or click the _Start_ button to run
1. [ ] The Aspire Dashboard will open in your default browser

### Using Terminal / VS Code

1. [ ] Open a terminal in the repository root
1. [ ] Run the following command:

```bash
dotnet run --project src/AgentEvalsWorkshop.AppHost
```

<details><summary>💡Dashboard Url</summary>
The terminal will display a URL for the Aspire Dashboard (typically <a href="https://localhost:15041">https://localhost:15041</a> or similar, along with an auth token in the uri). Open this URL in your browser, use the <b>CTRL+Click</b> shortcut to launch your default browser.
</details>

## Configure Azure AI Foundry Credentials

When the Aspire Dashboard opens, you'll need to provide your Azure subscription credentials to connect to Azure AI Foundry.

### Locate Your Azure Subscription ID

1. [ ] Navigate to the [Azure Portal](https://portal.azure.com)
1. [ ] Go to **Subscriptions** in the left navigation or search bar
1. [ ] Copy your **Subscription ID** from the list

### Input Credentials in Aspire Dashboard

1. [ ] In the Aspire Dashboard, locate the ++chat++ resource
1. [ ] Find the configuration prompt or parameters section
1. [ ] Paste your **Subscription ID** into the appropriate field

### Alternative: Configure via User Secrets

> [!knowledge] User Secrets
> 
> If you prefer to configure credentials outside the dashboard, use .NET User Secrets for a more persistent configuration.

1. [ ] Open a terminal and navigate to the AppHost project:

```bash
cd src/AgentEvalsWorkshop.AppHost
```

1. [ ] Set your Azure credentials using the following commands:

```bash
dotnet user-secrets set "Azure:SubscriptionId" "<your-subscription-id>"
dotnet user-secrets set "Azure:ResourceGroup" "<your-resource-group>"
dotnet user-secrets set "Azure:AIFoundry:Endpoint" "<your-foundry-endpoint>"
```

1. [ ] Restart the AppHost after configuring secrets

### Verify Connection

1. [ ] In the Aspire Dashboard, verify the ++chat++ resource transitions to a **Running** (green) state
1. [ ] Verify the ++agents++ resource shows as **Healthy**

> [+hint] 📚 **Documentation Links:**
>
> [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview)
>
> [Managing User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
>
> [Azure AI Foundry Setup](https://learn.microsoft.com/en-us/azure/ai-studio/quickstarts/get-started-playground)

## Verify Your Setup

Run a quick smoke test to ensure everything is configured correctly.

1. [ ] Open a new terminal in the repository root
1. [ ] Run the configuration smoke test:

```bash
dotnet test tests/AgentEvalsWorkshop.Tests --filter "FullyQualifiedName~ConfigurationSmokeTests"
```

1. [ ] Verify the test passes

> [!knowledge] Success Indicators
> 
> A passing test confirms:
> - ✅ Aspire AppHost starts successfully
> - ✅ Azure AI Foundry connection is established
> - ✅ Chat client can be resolved from the service provider

## Troubleshooting

> [!alert] Common Issues
> 
> | Symptom | Possible Cause | Resolution |
> |---------|----------------|------------|
> | AppHost fails to start | Docker not running | Start Docker Desktop and wait for it to be fully running |
> | ++chat++ resource stays in "Starting" | Missing or invalid Azure credentials | Verify Subscription ID and Foundry endpoint in user secrets or dashboard |
> | Connection timeout | Network/firewall issues | Ensure outbound access to Azure services; check VPN settings |
> | "Subscription not found" error | Incorrect Subscription ID | Double-check the ID in Azure Portal under Subscriptions |
> | Dashboard doesn't open | Port conflict | Check terminal output for the actual dashboard URL |

## Success Criteria

You're ready to proceed to the exercises when:

- [ ] Repository is cloned and solution opens without errors
- [ ] Aspire AppHost starts and the dashboard is accessible
- [ ] Azure credentials are configured (via dashboard or user secrets)
- [ ] The ++chat++ and ++agents++ resources show as Running/Healthy in the dashboard
- [ ] Configuration smoke tests pass

## Next Steps

Once your environment is set up, proceed to:

➡️ **[Exercise US1: Task Adherence Evaluation](US1-retrieval-tool.md)** - Create your first agent evaluation test using the TaskAdherenceEvaluator
