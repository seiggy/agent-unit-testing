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
- Solution open in Visual Studio 2026 or VS Code
- Azure Foundry endpoint configured in user secrets or environment variables

## Key Concepts

The **TaskAdherenceEvaluator** measures how well an AI agent follows instructions and uses its available tools to complete a task. It examines:
- Whether the agent called the appropriate tools
- Whether the agent's response aligns with the task requirements
- The quality of the agent's tool usage decisions

📚 **Documentation Links:**
- [TaskAdherenceEvaluator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.quality.taskadherenceevaluator?view=net-10.0-pp)
- [AI Evaluation Library Overview](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)

## Create the Test Class

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
<summary>💡 Show Example Implementation</summary>

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



##  Add the Reporting Configuration

1. [ ] Declare a static nullable field of type ++ReportingConfiguration++ to store the evaluation reporting configuration
> [!knowledge] This field will be initialized during class setup and shared across all tests in the class

> [+hint] 📚 **Documentation Links:**
>
> [Evaluate with Reporting Learn Tutorial](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting)

<details>
<summary>💡 Show Example Implementation</summary>

```csharp
private static ReportingConfiguration? s_defaultReportingConfiguration;
```

</details>

##  Create the Evaluators Factory

1. [ ] Create a *private static* method named ++GetEvaluators++ that returns `IEnumerable<IEvaluator>`
1. [ ] Instantiate a new **TaskAdherenceEvaluator** and return it in a collection
> [!knowledge] This method encapsulates which evaluators your tests will use

> [+hint] 📚 **Documentation Links:**
>
> [Sample Agent Evaluator Unit Tests](https://github.com/dotnet/extensions/blob/main/test/Libraries/Microsoft.Extensions.AI.Evaluation.Integration.Tests/AgentQualityEvaluatorTests.cs)

<details>
<summary>💡 Show Example Implementation</summary>

```csharp
private static IEnumerable<IEvaluator> GetEvaluators()
{
    var taskAdherenceEvaluator = new TaskAdherenceEvaluator();
    return [ taskAdherenceEvaluator ];
}
```

</details>

## Implement Class Initialization

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
<summary>💡 Show Example Implementation</summary>

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

## Write the Test Method

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
<summary>💡 Show Example Implementation</summary>

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

##  Run and Validate

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

## Generate and View the Evaluation Report

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
  - `eval-model`: The model used for evaluation (e.g., `gpt-4o-2024-11-20`)
  - `eval-input-tokens` / `eval-output-tokens` / `eval-total-tokens`: Token usage for the evaluation
  - `eval-duration-ms`: Time taken for the evaluation
- **Conversation Panel**: Collapsible section showing token counts and the full message exchange
- **Diagnostic Data Table**: Cache status, latency, model provider details, and token breakdown for each evaluation call

📚 **Documentation Links:**
- [aieval report tool](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting#generate-a-report)
- [Interpreting Evaluation Reports](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries#interpret-results)

---

## Success Criteria

A passing test indicates:
- ✅ The agent correctly identified it needed to retrieve weather information
- ✅ The agent called the `GetWeatherForecast` tool
- ✅ The agent's response addressed the user's question
- ✅ Task adherence rating is `Good` or `Exceptional`

---

## Troubleshooting

| Symptom | Possible Cause | Resolution |
|---------|----------------|------------|
| `NullReferenceException` on `s_defaultReportingConfiguration` | `ClassInitialize` didn't run | Verify the `[ClassInitialize]` attribute is present and the method signature is correct |
| `ServiceProvider` is `null` | Assembly initialization failed | Check that `BaseIntegrationTest.BaseInitialize` completed successfully; review Aspire app host logs |
| `ChatConfiguration` throws | Missing or invalid connection string | Verify Azure Foundry credentials in user secrets or environment variables |
| Low task adherence scores | Agent instructions don't align with task | Review the system prompt in `US1Agent.cs`; ensure tool definitions are correctly exposed |
| Report directory empty | Write permission denied | Verify your user has write access to the `storageRootPath` directory |

---

## Extension Challenges

1. **Multiple Evaluators**: Add `RelevanceEvaluator` or `CoherenceEvaluator` to your evaluator collection and observe how scores differ
2. **Edge Case Testing**: Create additional test methods for prompts like "What's the temperature in Celsius?" and evaluate how the agent handles unit conversion expectations
3. **Threshold Tuning**: Modify your assertions to require specific numeric score thresholds instead of rating enums
4. **Parameterized Tests**: Use `[DataRow]` to run the same test logic against multiple weather-related prompts

📚 **Documentation Links:**
- [Sample Evaluators](https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai-evaluation/api/evaluation/README.md)
- [Data Driven Unit Tests](https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=visualstudio)

---

## Complete Solution

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