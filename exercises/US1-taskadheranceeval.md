===
# Exercise: TaskAdherenceEvaluator

## Goal
Create a test class that uses the **TaskAdherenceEvaluator** to evaluate how well an AI agent accomplishes a given task. You will build a Weather Assistant Agent test that verifies the agent correctly uses its available tools to respond to user requests.

## Learning Objectives
- Understand how to configure AI evaluation reporting
- Learn to use the _TaskAdherenceEvaluator_ to measure agent performance
- Practice writing integration tests for AI agents
- Interpret evaluation metrics and assertions

## Prerequisites
- Aspire AppHost configured run once
- Azure Foundry endpoint configured (🟦 .NET: user secrets or environment variables · 🐍 Python: `.env` + `az login`)

**🟦 .NET**
- Solution open in Visual Studio 2026 or VS Code

**🐍 Python**
- `src-python/` opened in VS Code with `uv sync` completed
- `.env` configured (copied from `.env.example`) and `az login` done

## Key Concepts

The **TaskAdherenceEvaluator** measures how well an AI agent follows instructions and uses its available tools to complete a task. It examines:
- Whether the agent called the appropriate tools
- Whether the agent's response aligns with the task requirements
- The quality of the agent's tool usage decisions

Both tracks use the same evaluator concept: the **🟦 .NET** track uses
`Microsoft.Extensions.AI.Evaluation.Quality.TaskAdherenceEvaluator`, while the **🐍 Python** track uses
`azure.ai.evaluation.TaskAdherenceEvaluator`. Both score task adherence on a 1–5 scale.

📚 **Documentation Links:**
- [TaskAdherenceEvaluator Documentation (.NET)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.quality.taskadherenceevaluator?view=net-10.0-pp)
- [AI Evaluation Library Overview (.NET)](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)
- [azure-ai-evaluation SDK (Python)](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)

## Create the Test Class

**🟦 .NET**

1. [ ] Create a new file **WeatherAssistantAgentTests.cs** in the **tests/AgentEvalsWorkshop.Tests/** project.

1. [ ] Add the necessary **using** directives for the ++AgentEvalsWorkshop.Agents++ namespace, test helpers, and the ++Microsoft.Extensions.AI.Evaluation++ packages (including `Quality`, `Reporting`, and `Reporting.Storage` namespaces)
1. [ ] Define a test class decorated with ++[TestClass]++ that inherits from ++BaseIntegrationTest++

> [!knowledge] BaseIntegrationTest
> 
> The base class provides a **ServiceProvider**, **ChatConfiguration**, **ScenarioName**, **ExecutionName**, and **Evaluators**
> that will be used in your tests.

> [+hint] 📚 **Documentation Links:**
>
> [MSTest v3](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests)

<details>
<summary>💡 Show Example Implementation (🟦 .NET)</summary>

```csharp
using AgentEvalsWorkshop.Agents;
using AgentEvalsWorkshop.Tests.Helpers;
using Aspire.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class WeatherAssistantAgentTests : BaseIntegrationTest
{
    // Fields and methods will be added in subsequent steps
}
```

</details>

**🐍 Python**

1. [ ] Open **tests-python/test_weather_assistant_agent.py** — the skeleton is already scaffolded for you (a near-empty test module, the Python parity of the empty `WeatherAssistantAgentTests.cs`).

1. [ ] Add imports for the agent's public API (`agent_evals_workshop.agents.us1_agent`), the config module, and the evaluator-input helpers (`helpers.agent_run`).
1. [ ] Instead of a `[TestClass]` inheriting from `BaseIntegrationTest`, write an **async test function** that consumes pytest **fixtures**. The fixtures (defined in `tests-python/conftest.py`) are the Python parity of the .NET base class.

> [!knowledge] Fixtures replace BaseIntegrationTest
> 
> `conftest.py` provides fixtures — **settings**, **chat_client**, **model_config**, **project_client**, and
> **scenario_name** — that are injected into your test by name. This is the Python-idiomatic equivalent of
> the .NET `BaseIntegrationTest` providing `ServiceProvider`, `ChatConfiguration`, `ScenarioName`, etc.

> [+hint] 📚 **Documentation Links:**
>
> [pytest fixtures](https://docs.pytest.org/en/stable/how-to/fixtures.html)
>
> [pytest-asyncio](https://pytest-asyncio.readthedocs.io/en/latest/)

<details>
<summary>💡 Show Example Implementation (🐍 Python)</summary>

```python
from __future__ import annotations

import pytest

from agent_evals_workshop import config
from agent_evals_workshop.agents.us1_agent import build_us1_agent, get_tool_definitions
from helpers.agent_run import to_eval_inputs, write_jsonl

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
    # Arrange / Act / Assert / Report added in subsequent steps
    ...
```

</details>



##  Add the Reporting Configuration

**🟦 .NET**

1. [ ] Declare a static nullable field of type ++ReportingConfiguration++ to store the evaluation reporting configuration
> [!knowledge] This field will be initialized during class setup and shared across all tests in the class

> [+hint] 📚 **Documentation Links:**
>
> [Evaluate with Reporting Learn Tutorial](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting)

<details>
<summary>💡 Show Example Implementation (🟦 .NET)</summary>

```csharp
private static ReportingConfiguration? s_defaultReportingConfiguration;
```

</details>

**🐍 Python**

> [!knowledge] No ReportingConfiguration needed
> 
> The Python track does **not** use a `ReportingConfiguration` object. Reporting happens in two ways:
> a fast **local gate** by calling the evaluator directly (covered next), and **portal visibility** via
> `evaluate(..., azure_ai_project=...)` at the end of the test (see *Generate and View the Evaluation
> Report*). There is nothing to declare here — skip ahead to the evaluator.

##  Create the Evaluators Factory

**🟦 .NET**

1. [ ] Create a *private static* method named ++GetEvaluators++ that returns `IEnumerable<IEvaluator>`
1. [ ] Instantiate a new **TaskAdherenceEvaluator** and return it in a collection
> [!knowledge] This method encapsulates which evaluators your tests will use

> [+hint] 📚 **Documentation Links:**
>
> [Sample Agent Evaluator Unit Tests](https://github.com/dotnet/extensions/blob/main/test/Libraries/Microsoft.Extensions.AI.Evaluation.Integration.Tests/AgentQualityEvaluatorTests.cs)

<details>
<summary>💡 Show Example Implementation (🟦 .NET)</summary>

```csharp
private static IEnumerable<IEvaluator> GetEvaluators()
{
    var taskAdherenceEvaluator = new TaskAdherenceEvaluator();
    return [ taskAdherenceEvaluator ];
}
```

</details>

**🐍 Python**

1. [ ] The Python `TaskAdherenceEvaluator` is a **callable object** constructed with a judge `model_config`. Rather than a factory method, you construct it inline in the test using the `model_config` fixture.
> [!knowledge] The `model_config` fixture supplies an `AzureOpenAIModelConfiguration` (endpoint + `azure_deployment="chat"` + api version) that the evaluator uses as its LLM judge.

> [+hint] 📚 **Documentation Links:**
>
> [azure-ai-evaluation TaskAdherenceEvaluator](https://learn.microsoft.com/en-us/python/api/azure-ai-evaluation/azure.ai.evaluation.taskadherenceevaluator)

<details>
<summary>💡 Show Example Implementation (🐍 Python)</summary>

```python
from azure.ai.evaluation import TaskAdherenceEvaluator

task_adherence = TaskAdherenceEvaluator(model_config)
```

</details>

## Implement Class Initialization

**🟦 .NET**

1. [ ] Add a `public static async Task` method decorated with `[ClassInitialize]` that accepts a `TestContext` parameter
1. [ ] Assign the result of `GetEvaluators()` to the inherited `Evaluators` property
1. [ ] Use `DiskBasedReportingConfiguration.Create()` to configure reporting with:
  - A `storageRootPath` for persisting HTML reports (e.g., `C:\TestReports`)
  - Your `Evaluators` collection
  - The inherited `ChatConfiguration` for evaluator LLM calls
  - `enableResponseCaching` set to `true` for deterministic reruns
  - The inherited `ExecutionName` to group test runs

> [+hint] 📚 **Documentation Links:**
>
> [Unit Testing Lifecycles](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests-lifecycle#class-level-lifecycle)
>
> [DiskBasedReportingConfiguration Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.reporting.storage.diskbasedreportingconfiguration?view=net-10.0-pp)

<details>
<summary>💡 Show Example Implementation (🟦 .NET)</summary>

```csharp
[ClassInitialize]
public static async Task ClassInitialize(TestContext context)
{
    Evaluators = GetEvaluators();
    s_defaultReportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: "C:\\TestReports",
            evaluators: Evaluators,
            chatConfiguration: ChatConfiguration,
            enableResponseCaching: true,
            executionName: ExecutionName
        );
}
```

</details>

**🐍 Python**

The Python track has **no `[ClassInitialize]`**. Setup that the .NET base class performs once per class is
provided by pytest **fixtures** in `tests-python/conftest.py`, resolved fresh per test by dependency
injection. You don't write initialization code — you just **declare the fixtures you need** as parameters.

1. [ ] Confirm your test function requests the fixtures it needs: `chat_client`, `model_config`, `settings`, `project_client`, `scenario_name`, and `tmp_path`.
> [!knowledge] The `foundry` marker (added in the next step) auto-skips the test offline when `AZURE_OPENAI_ENDPOINT` is unset, mirroring the .NET track's opt-in-cloud behavior.

> [+hint] 📚 **Documentation Links:**
>
> [pytest conftest.py & fixture scope](https://docs.pytest.org/en/stable/reference/fixtures.html)

<details>
<summary>💡 Show Example Implementation (🐍 Python)</summary>

```python
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
    ...  # Arrange / Act / Assert / Report below
```

</details>

## Write the Test Method

**🟦 .NET**

### Arrange
1. [ ] Decorate your method with `[TestMethod]` and make it `public async Task`
1. [ ] Create a `ScenarioRun` by calling `CreateScenarioRunAsync()` on your reporting configuration, passing `ScenarioName` and optional tags (use `await using` for proper disposal)
1. [ ] Create a scoped `IServiceProvider` from the inherited `ServiceProvider` and resolve an `IChatClient`
1. [ ] Build the agent using `US1Agent.BuildUS1Agent()`, passing the chat client
1. [ ] Create a `TaskAdherenceEvaluatorContext` with the agent's tool definitions from `US1Agent.GetToolDefinitions()`

### Act
1. [ ] Define a user message string that should trigger the weather tool (e.g., asking about weather)
1. [ ] Call `agent.RunAsync()` with the message and a cancellation token from `TestContext.CancellationTokenSource`
1. [ ] Call `scenarioRun.EvaluateAsync()` passing:
  - The `Messages` from the agent response
  - The response converted via the `ToChatResponse()` extension method
  - The tool definitions context in the `additionalContext` array
  - The cancellation token

### Assert
1. [ ] Extract the `NumericMetric` from results using `result.Get<NumericMetric>()` with `TaskAdherenceEvaluator.TaskAdherenceMetricName`
1. [ ] Assert that `Interpretation.Failed` is `false` (use the metric's `Reason` as the failure message)
1. [ ] Assert that `Interpretation.Rating` is either `EvaluationRating.Good` or `EvaluationRating.Exceptional`

> [+hint] 📚 **Documentation Links:**
>
> [ScenarioRun Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.reporting.reportingconfiguration.createscenariorunasync?view=net-10.0-pp)
>
> [Evaluator Examples](https://github.com/dotnet/ai-samples/tree/main/src/microsoft-extensions-ai-evaluation/api/evaluation)

<details>
<summary>💡 Show Example Implementation (🟦 .NET)</summary>

```csharp
[TestMethod]
public async Task DoesPersonalAgentRetrieveWeather()
{
    // Arrange
    await using ScenarioRun scenarioRun =
        await s_defaultReportingConfiguration!
        .CreateScenarioRunAsync(ScenarioName,
        additionalTags: ["Weather", "Agent"]);
    using var scope = ServiceProvider!.CreateScope();
    var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
    var agent = US1Agent.BuildUS1Agent(chatClient);

    var toolDefinitionsForTaskAdherenceEvaluator =
        new TaskAdherenceEvaluatorContext(toolDefinitions: US1Agent.GetToolDefinitions());

    // Act
    var userMessage = "What's the weather like today?";
    var response = await agent.RunAsync(userMessage,
        cancellationToken: TestContext!.CancellationTokenSource.Token);

    var result = await scenarioRun.EvaluateAsync(
        messages: response.Messages,
        modelResponse: response.ToChatResponse(),
        additionalContext: [toolDefinitionsForTaskAdherenceEvaluator],
        cancellationToken: TestContext!.CancellationTokenSource.Token);

    // Assert
    NumericMetric taskAdherance = result.Get<NumericMetric>(TaskAdherenceEvaluator.TaskAdherenceMetricName);
    Assert.IsFalse(taskAdherance.Interpretation!.Failed, taskAdherance.Reason);
    Assert.IsTrue(taskAdherance.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
}
```

</details>

**🐍 Python**

### Arrange
1. [ ] Build the agent with `build_us1_agent(chat_client)` (the `chat_client` fixture provides the Azure OpenAI client).
1. [ ] Define a user message that should trigger the weather tool (e.g., asking about the weather).

### Act
1. [ ] Run the agent with `response = await agent.run(query)`.
1. [ ] Adapt the result into evaluator inputs with `to_eval_inputs(response, query=query, tool_definitions=get_tool_definitions())` — this is the Python parity of `ToChatResponse()` + `TaskAdherenceEvaluatorContext`.

### Assert
1. [ ] Construct the evaluator: `task_adherence = TaskAdherenceEvaluator(model_config)`.
1. [ ] Call it with the adapted inputs: `score = task_adherence(query=..., response=..., tool_definitions=...)`.
1. [ ] Assert the result **is not a failure**: `assert score["task_adherence_result"] != "fail", score["task_adherence_reason"]`.
1. [ ] Assert the numeric score is in the Good/Exceptional band: `assert score["task_adherence"] >= 4, score["task_adherence_reason"]`.

> [!knowledge] Result-dict keys
> 
> The Python evaluator returns a flat dict keyed by the evaluator's result key:
> - `task_adherence` — the numeric score (1–5); the `>= 4` gate is the Good/Exceptional band.
> - `task_adherence_result` — a pass/fail verdict string (assert it is not `"fail"`).
> - `task_adherence_reason` — the judge's explanation; use it as the assertion message.

> [+hint] 📚 **Documentation Links:**
>
> [Run evaluators locally (azure-ai-evaluation)](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)

<details>
<summary>💡 Show Example Implementation (🐍 Python)</summary>

```python
from azure.ai.evaluation import TaskAdherenceEvaluator

# Arrange
agent = build_us1_agent(chat_client)
query = "What's the weather like today?"

# Act
response = await agent.run(query)
inputs = to_eval_inputs(response, query=query, tool_definitions=get_tool_definitions())

# Assert — local, deterministic gate via the callable evaluator.
task_adherence = TaskAdherenceEvaluator(model_config)
score = task_adherence(
    query=inputs.query,
    response=inputs.response,
    tool_definitions=inputs.tool_definitions,
)
assert score["task_adherence_result"] != "fail", score["task_adherence_reason"]
assert score["task_adherence"] >= 4, score["task_adherence_reason"]
```

</details>

##  Run and Validate

**🟦 .NET**

1. [ ] Build the solution to verify there are no compilation errors
1. [ ] Execute the test using the `dotnet test` CLI with a filter for your test class, or use Visual Studio Test Explorer

	```Powershell
	dotnet test tests/AgentEvalsWorkshop.Tests --filter "FullyQualifiedName~WeatherAssistantAgentTests"
	```

	@[Click this to run the unit tests][Run Unit Tests]{Powershell}

[Run Unit Tests]:
```Powershell
cd c:\github\agent-unit-testing
dotnet test tests/AgentEvalsWorkshop.Tests --filter "FullyQualifiedName~WeatherAssistantAgentTests"
```

> [+hint] 📚 **Documentation Links:**
>
> [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest)

**🐍 Python**

1. [ ] From `src-python/` (where the uv environment lives), run the weather test with the `foundry` marker:

	```Powershell
	uv run pytest ../tests-python -k weather -m foundry
	```

	@[Click this to run the Python test][Run Python Test]{Powershell}

[Run Python Test]:
```Powershell
cd c:\github\agent-unit-testing\src-python
uv run pytest ../tests-python -k weather -m foundry
```

> [!knowledge] The `-m foundry` marker selects the live-config test. If `AZURE_OPENAI_ENDPOINT` is unset the test **skips** instead of failing — make sure your `.env` is filled in and you have run `az login`.

> [+hint] 📚 **Documentation Links:**
>
> [pytest marker selection (`-m`)](https://docs.pytest.org/en/stable/example/markers.html)

## Generate and View the Evaluation Report

**🟦 .NET**

1. [ ] After the test completes successfully, use the `aieval` CLI tool to generate an HTML report from the cached evaluation data
1. [ ] Run the following command from the root of your repository to generate a consolidated report:

	```Powershell
	cd c:\github\agent-unit-testing
	dotnet aieval report -p C:\TestReports -o test-report.html
	```

	@[Click this to run the test report][AIEval Report]{Powershell}

[AIEval Report]:
```Powershell
cd c:\github\agent-unit-testing
dotnet aieval report -p C:\TestReports -o test-report.html
```

3. [ ] Open `test-report.html` in your browser to view the detailed evaluation results

**What you'll see in the report:**

!IMAGE[weather-agent-eval-report.png](instructions333020/weather-agent-eval-report.png)

- **Breadcrumb Navigation**: Hierarchical path showing All Evaluations → Namespace → Test Class → Test Method with pass/fail counts (e.g., `1/1 [100.0%]`)
- **Trends Panel**: Collapsible section for viewing evaluation trends over time
- **Task Adherence Score**: A highlighted metric card showing the numeric score (e.g., `5`) with a visual indicator of success
- **Metric Details**: Expanded view containing:
  - **Evaluation Reason**: Plain-language explanation of why the score was assigned (e.g., "The response is clear, accurate, and adheres to the instructions by retrieving the weather data...")
  - **Diagnostics**: The evaluator's chain-of-thought reasoning showing step-by-step analysis of tool usage and response quality
- **Metadata Table**: Key-value pairs including:
  - `built-in-eval`: Whether the evaluator is built-in
  - `eval-model`: The model used for evaluation (e.g., `gpt-5.4`)
  - `eval-input-tokens` / `eval-output-tokens` / `eval-total-tokens`: Token usage for the evaluation
  - `eval-duration-ms`: Time taken for the evaluation
- **Conversation Panel**: Collapsible section showing token counts and the full message exchange
- **Diagnostic Data Table**: Cache status, latency, model provider details, and token breakdown for each evaluation call

📚 **Documentation Links:**
- [aieval report tool](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting#generate-a-report)
- [Interpreting Evaluation Reports](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries#interpret-results)

**🐍 Python**

The Python track does **not** produce a local HTML file. Instead it uploads the run to the **Azure AI
Foundry portal**, where you view results in the **Evaluations** tab. This happens by calling `evaluate()`
with your Foundry **project** endpoint at the end of the test.

1. [ ] After the local assertions pass, write the evaluator inputs to a JSONL dataset and call `evaluate()`, pointing `azure_ai_project` at your `AZURE_AI_PROJECT_ENDPOINT` (supplied by the `settings` fixture):

	```python
	from azure.ai.evaluation import evaluate

	jsonl_path = write_jsonl(inputs, tmp_path / "us1.jsonl")
	eval_result = evaluate(
	    data=str(jsonl_path),
	    evaluation_name=scenario_name,
	    evaluators={"task_adherence": task_adherence},
	    azure_ai_project=settings.azure_ai_project_endpoint,
	    output_path=str(tmp_path / "us1-eval.json"),
	)
	studio_url = eval_result.get("studio_url")
	print(f"Foundry Studio: {studio_url}")
	```

1. [ ] Copy the printed **`Foundry Studio:`** URL from the test output and open it in your browser.
1. [ ] In the [Azure AI Foundry portal](https://ai.azure.com), open the **Evaluations** tab for your project to inspect the run.

**What you'll see in the Foundry Evaluations tab:**

<!-- TODO screenshot: Foundry Evaluations tab showing the US1 task_adherence run -->

- **Evaluation run**: named by `scenario_name`, listed under your project's **Evaluations** tab
- **Task Adherence Score**: the numeric score (1–5) with the pass/fail result
- **Reason**: the judge's plain-language explanation (the same `task_adherence_reason` used in your assertion message)
- **Token usage & metrics**: aggregated metrics returned in `eval_result["metrics"]` and shown in the portal

📚 **Documentation Links:**
- [Run cloud evaluations & view in the portal](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/cloud-evaluation)
- [Evaluate with the azure-ai-evaluation SDK](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)

---

## Success Criteria

A passing test indicates:
- ✅ The agent correctly identified it needed to retrieve weather information
- ✅ The agent called the weather tool (🟦 .NET: `GetWeatherForecast` · 🐍 Python: `get_weather_forecast`)
- ✅ The agent's response addressed the user's question
- ✅ Task adherence is in the pass band (🟦 .NET: rating `Good` or `Exceptional` · 🐍 Python: `task_adherence >= 4` and `task_adherence_result != "fail"`)

---

## Troubleshooting

**🟦 .NET**

| Symptom | Possible Cause | Resolution |
|---------|----------------|------------|
| `NullReferenceException` on `s_defaultReportingConfiguration` | `ClassInitialize` didn't run | Verify the `[ClassInitialize]` attribute is present and the method signature is correct |
| `ServiceProvider` is `null` | Assembly initialization failed | Check that `BaseIntegrationTest.BaseInitialize` completed successfully; review Aspire app host logs |
| `ChatConfiguration` throws | Missing or invalid connection string | Verify Azure Foundry credentials in user secrets or environment variables |
| Low task adherence scores | Agent instructions don't align with task | Review the system prompt in `US1Agent.cs`; ensure tool definitions are correctly exposed |
| Report directory empty | Write permission denied | Verify your user has write access to the `storageRootPath` directory |

**🐍 Python**

| Symptom | Possible Cause | Resolution |
|---------|----------------|------------|
| Test is **skipped** | `AZURE_OPENAI_ENDPOINT` unset (the `foundry` marker skips offline) | Fill in `src-python/.env` and run `az login` |
| `ModuleNotFoundError` / import errors | uv environment not restored | Run `uv sync` in `src-python/`; run tests via `uv run pytest` |
| Auth / token errors | Not signed in to Azure CLI | Run `az login`; `DefaultAzureCredential` needs an active CLI session |
| `evaluate()` upload fails | `AZURE_AI_PROJECT_ENDPOINT` missing or wrong | Set the Foundry **project** endpoint in `.env` (not the plain Azure OpenAI endpoint) |
| Low `task_adherence` score | Agent instructions don't align with task | Review the instructions in `us1_agent.py`; ensure `get_tool_definitions()` is passed to the evaluator |

---

## Extension Challenges

1. **Multiple Evaluators**: Add another evaluator (🟦 .NET: `RelevanceEvaluator` / `CoherenceEvaluator` · 🐍 Python: `ToolCallAccuracyEvaluator` / `RelevanceEvaluator`) and observe how scores differ
2. **Edge Case Testing**: Add tests for prompts like "What's the temperature in Celsius?" and evaluate how the agent handles unit conversion expectations
3. **Threshold Tuning**: Modify your assertions to require specific numeric score thresholds
4. **Parameterized Tests**: Run the same test logic against multiple weather-related prompts (🟦 .NET: `[DataRow]` · 🐍 Python: `@pytest.mark.parametrize`)

📚 **Documentation Links:**
- [Sample Evaluators (.NET)](https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai-evaluation/api/evaluation/README.md)
- [Data Driven Unit Tests (.NET)](https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=visualstudio)
- [Parametrizing tests (pytest)](https://docs.pytest.org/en/stable/how-to/parametrize.html)

---

## Complete Solution

**🟦 .NET**

<details>
<summary>📄 Show Complete WeatherAssistantAgentTests.cs</summary>

```csharp
using AgentEvalsWorkshop.Agents;
using AgentEvalsWorkshop.Tests.Helpers;
using Aspire.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class WeatherAssistantAgentTests : BaseIntegrationTest
{
    private static ReportingConfiguration? s_defaultReportingConfiguration;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Evaluators = GetEvaluators();
        s_defaultReportingConfiguration = DiskBasedReportingConfiguration.Create(
                storageRootPath: "C:\\TestReports",
                evaluators: Evaluators,
                chatConfiguration: ChatConfiguration,
                enableResponseCaching: true,
                executionName: ExecutionName
            );
    }

    private static IEnumerable<IEvaluator> GetEvaluators()
    {
        var taskAdherenceEvaluator = new TaskAdherenceEvaluator();
        return [ taskAdherenceEvaluator ];
    }

    [TestMethod]
    public async Task DoesPersonalAgentRetrieveWeather()
    {
        // Arrange
        await using ScenarioRun scenarioRun =
            await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync(ScenarioName,
            additionalTags: ["Weather", "Agent"]);
        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = US1Agent.BuildUS1Agent(chatClient);

        var toolDefinitionsForTaskAdherenceEvaluator =
            new TaskAdherenceEvaluatorContext(toolDefinitions: US1Agent.GetToolDefinitions());

        // Act
        var userMessage = "What's the weather like today?";
        var response = await agent.RunAsync(userMessage,
            cancellationToken: TestContext!.CancellationTokenSource.Token);

        var result = await scenarioRun.EvaluateAsync(
            messages: response.Messages,
            modelResponse: response.ToChatResponse(),
            additionalContext: [toolDefinitionsForTaskAdherenceEvaluator],
            cancellationToken: TestContext!.CancellationTokenSource.Token);

        // Assert
        NumericMetric taskAdherance = result.Get<NumericMetric>(TaskAdherenceEvaluator.TaskAdherenceMetricName);
        Assert.IsFalse(taskAdherance.Interpretation!.Failed, taskAdherance.Reason);
        Assert.IsTrue(taskAdherance.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
    }
}
```

</details>

**🐍 Python**

<details>
<summary>📄 Show Complete test_weather_assistant_agent.py</summary>

```python
"""US1 weather-assistant task-adherence eval (SOLUTION reference).

Python-idiomatic equivalent of ``solutions/WeatherAssistantAgentTests.cs``:

* **Arrange** — build the US1 ``Assistant`` agent (``build_us1_agent``).
* **Act** — ``await agent.run("What's the weather like today?")`` and adapt the
  result into evaluator inputs (query / response / tool_definitions).
* **Assert** — score with ``TaskAdherenceEvaluator(model_config)`` and require the
  Good/Exceptional band (numeric score ≥ 4 on the 1–5 scale), the local gate.
* **Report** — push the run to the Azure AI Foundry portal with
  ``evaluate(..., azure_ai_project=...)`` and print the returned Studio URL
  (replaces the .NET ``aieval`` HTML report / ``C:\\TestReports`` flow).

Marked ``@pytest.mark.foundry`` — auto-skipped offline by the ``conftest`` hook.
"""

from __future__ import annotations

import pytest

from agent_evals_workshop import config
from agent_evals_workshop.agents.us1_agent import build_us1_agent, get_tool_definitions
from helpers.agent_run import to_eval_inputs, write_jsonl

# TaskAdherenceEvaluator scores 1–5. "Good"/"Exceptional" ≈ score >= 4
# (parity with the .NET EvaluationRating.Good / .Exceptional gate).
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
    from azure.ai.evaluation import TaskAdherenceEvaluator, evaluate

    # Arrange
    agent = build_us1_agent(chat_client)
    query = "What's the weather like today?"

    # Act
    response = await agent.run(query)
    inputs = to_eval_inputs(response, query=query, tool_definitions=get_tool_definitions())

    # Assert — local, deterministic gate via the callable evaluator.
    task_adherence = TaskAdherenceEvaluator(model_config)
    score = task_adherence(
        query=inputs.query,
        response=inputs.response,
        tool_definitions=inputs.tool_definitions,
    )
    # Result-dict keys are prefixed with the evaluator result key "task_adherence".
    assert score["task_adherence_result"] != "fail", score["task_adherence_reason"]
    assert score["task_adherence"] >= GOOD_ADHERENCE_THRESHOLD, score["task_adherence_reason"]

    # Report — upload the run to the Azure AI Foundry portal (Evaluations tab).
    jsonl_path = write_jsonl(inputs, tmp_path / "us1.jsonl")
    eval_result = evaluate(
        data=str(jsonl_path),
        evaluation_name=scenario_name,
        evaluators={"task_adherence": task_adherence},
        azure_ai_project=settings.azure_ai_project_endpoint,
        output_path=str(tmp_path / "us1-eval.json"),
    )
    studio_url = eval_result.get("studio_url")
    print(f"Foundry Studio: {studio_url}")
    assert eval_result["metrics"], "evaluate() returned no aggregated metrics."
```

</details>