# Exercise: Retrieval Evaluation with Built-in Evaluators

## Goal
Create a test class that evaluates a Knowledgebase Chat Agent using **built-in evaluators** (Relevance, Coherence, Groundedness). You will learn how to use multiple evaluators together to comprehensively assess AI response quality against a knowledge base.

## Learning Objectives
- Understand how to use multiple built-in evaluators together for comprehensive evaluation
- Use **data-driven tests** to evaluate multiple question/answer scenarios
- Work with **GroundednessEvaluatorContext** to validate responses against a knowledge base
- Interpret evaluation metrics from multiple evaluators simultaneously

## Prerequisites
- Exercise US1 completed successfully
- Aspire AppHost configured and running
- Solution open in Visual Studio 2026 or VS Code
- Azure Foundry endpoint configured in user secrets or environment variables

## Key Concepts

This exercise combines several built-in evaluators to assess AI response quality:

| Evaluator | Purpose |
|-----------|---------|
| **RelevanceEvaluator** | Measures how relevant the response is to the user's question |
| **CoherenceEvaluator** | Measures how well-structured and coherent the response is |
| **GroundednessEvaluator** | Measures whether the response is grounded in provided context/facts |

📚 **Documentation Links:**
- [RelevanceEvaluator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.quality.relevanceevaluator?view=net-10.0-pp)
- [CoherenceEvaluator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.quality.coherenceevaluator?view=net-10.0-pp)
- [GroundednessEvaluator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.quality.groundednessevaluator?view=net-10.0-pp)

---

## Create the Test File

1. [ ] Create a new file **AgentRetrievalEvalTests.cs** in the **tests/AgentEvalsWorkshop.Tests/** project

1. [ ] Add the necessary **using** directives:
   - `AgentEvalsWorkshop.Agents` for the agent
   - `AgentEvalsWorkshop.Tests.Helpers` for test helpers
   - `Microsoft.Agents.AI` for the AI agent framework
   - `Microsoft.Extensions.AI` for chat client interfaces
   - `Microsoft.Extensions.AI.Evaluation` and sub-namespaces for evaluators
   - Add `ChatRole = Microsoft.Extensions.AI.ChatRole` alias to avoid ambiguity

1. [ ] Add the pragma directive `#pragma warning disable AIEVAL001` at the top to suppress preview API warnings

1. [ ] Define a test class decorated with `[TestClass]` that inherits from `BaseIntegrationTest`

<details>
<summary>💡 Show Class Setup</summary>

```csharp
#pragma warning disable AIEVAL001
using AgentEvalsWorkshop.Agents;
using AgentEvalsWorkshop.Tests.Helpers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class AgentRetrievalEvalTests : BaseIntegrationTest
{
    private static ReportingConfiguration? s_defaultReportingConfiguration;
    
    // Methods will be added in subsequent steps
}
```

</details>

## Add the Reporting Configuration

1. [ ] Declare a static nullable field of type `ReportingConfiguration` to store the evaluation reporting configuration

> [!knowledge] This field will be initialized during class setup and shared across all tests in the class

<details>
<summary>💡 Show Field Declaration</summary>

```csharp
private static ReportingConfiguration? s_defaultReportingConfiguration;
```

</details>

## Create the Evaluators Factory

1. [ ] Create a `private static` method named `GetEvaluators` that returns `IEnumerable<IEvaluator>`
1. [ ] Instantiate the three built-in evaluators:
   - `RelevanceEvaluator`
   - `CoherenceEvaluator`
   - `GroundednessEvaluator`
1. [ ] Return them in a collection

<details>
<summary>💡 Show GetEvaluators Implementation</summary>

```csharp
private static IEnumerable<IEvaluator> GetEvaluators()
{
    var relevanceEvaluator = new RelevanceEvaluator();
    var coherenceEvaluator = new CoherenceEvaluator();
    var groundednessEvaluator = new GroundednessEvaluator();
    return [ relevanceEvaluator, coherenceEvaluator, groundednessEvaluator ];
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
<summary>💡 Show ClassInitialize Implementation</summary>

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

## Create the Knowledge Base Helper

1. [ ] Create a `private static async Task<string>` method named `GetKnowledgebaseContext`
1. [ ] Read the CSV file from `./Data/Gamepass_Games_v1.csv`
1. [ ] Return a formatted string containing the knowledge base context

> [!knowledge] GroundednessEvaluator Context
> 
> The `GroundednessEvaluator` needs a reference to the source data to verify the AI's response is grounded in facts. This helper provides that context.

<details>
<summary>💡 Show GetKnowledgebaseContext Implementation</summary>

```csharp
private static async Task<string> GetKnowledgebaseContext()
{
    var csvData = await File.ReadAllLinesAsync("./Data/Gamepass_Games_v1.csv");

    return $"""
        The following is the knowledge base about Xbox Gamepass games in CSV format:
        {string.Join("\n", csvData)}
        """;
}
```

</details>

## Create the EvalQuestion Record

1. [ ] Add an `EvalQuestion` record to hold question/answer pairs for testing:

```csharp
record EvalQuestion(int QuestionId, string Question, string Answer);
```

## Create the Evaluation Helper Method

1. [ ] Create a `private static async Task` method named `EvaluateQuestion` with parameters:
   - `EvalQuestion question`
   - `ReportingConfiguration reportingConfiguration`
   - `AIAgent agent`
   - `CancellationToken cancellationToken`

1. [ ] Create a `ScenarioRun` for each question using `CreateScenarioRunAsync()`

1. [ ] Create a new thread for the agent and build the chat history

1. [ ] Run the agent and collect the response

1. [ ] Call `scenario.EvaluateAsync()` passing:
   - The user's question as a `ChatMessage`
   - The agent response converted via `ToChatResponse()`
   - Additional context with `GroundednessEvaluatorContext` containing the knowledge base

<details>
<summary>💡 Show EvaluateQuestion Implementation</summary>

```csharp
private static async Task EvaluateQuestion(
    EvalQuestion question, 
    ReportingConfiguration reportingConfiguration, 
    AIAgent agent,
    CancellationToken cancellationToken)
{
    // Create a Scenario Run for each question.
    await using ScenarioRun scenario = await reportingConfiguration.CreateScenarioRunAsync($"Question_{question.QuestionId}", cancellationToken: cancellationToken);

    // Create a session for the agent using CreateSessionAsync to track the Q&A interaction
    var session = await agent.CreateSessionAsync();
    var chatHistory = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, question.Question)
    };

    var response = await agent.RunAsync(
        chatHistory,
        session: session,
        cancellationToken: cancellationToken
    );
    chatHistory.AddRange(response.Messages);

    var result = await scenario.EvaluateAsync(
        messages: [new ChatMessage(ChatRole.User, question.Question)],
        modelResponse: response.ToChatResponse(),
        additionalContext: [new GroundednessEvaluatorContext(await GetKnowledgebaseContext())],
        cancellationToken: cancellationToken
    );

    Validate(result);
}
```

</details>

## Create the Validation Helper

1. [ ] Create a `private static void` method named `Validate` that accepts an `EvaluationResult`
1. [ ] Retrieve and assert on all three metrics:
   - **Relevance**: Assert not failed and rating is `Good` or `Exceptional`
   - **Coherence**: Assert not failed and rating is `Good` or `Exceptional`
   - **Groundedness**: Assert not failed and rating is `Good` or `Exceptional`

<details>
<summary>💡 Show Validate Implementation</summary>

```csharp
private static void Validate(EvaluationResult result)
{
    // Retrieve the score for relevance from the EvaluationResult.
    NumericMetric relevance =
        result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
    Assert.IsFalse(relevance.Interpretation?.Failed, relevance.Reason);
    Assert.IsTrue(relevance.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

    // Retrieve the score for coherence from the EvaluationResult.
    NumericMetric coherence =
        result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
    Assert.IsFalse(coherence.Interpretation?.Failed, coherence.Reason);
    Assert.IsTrue(coherence.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
            
    // Retrieve the score for groundedness from the EvaluationResult.
    NumericMetric groundedness =
        result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
    Assert.IsFalse(groundedness.Interpretation?.Failed, groundedness.Reason);
    Assert.IsTrue(groundedness.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
}
```

</details>

## Write the Data-Driven Test Method

1. [ ] Decorate your method with `[TestMethod]` and multiple `[DataRow]` attributes for different test cases
1. [ ] Accept parameters: `int questionId`, `string questionText`, `string expectedAnswer`
1. [ ] Create an `EvalQuestion` record from the parameters
1. [ ] Resolve the `IChatClient` and build the `KnowledgebaseChatAgent`
1. [ ] Call `EvaluateQuestion()` with all dependencies

> [!knowledge] Data-Driven Tests
> 
> Using `[DataRow]` attributes allows you to run the same test logic against multiple input/output scenarios without duplicating code.

<details>
<summary>💡 Show Test Method Implementation</summary>

```csharp
[TestMethod]
[DataRow(1, "What game can I quickly play to get 1000 gamerscore?", "The shortest known game to achieve 1000 gamerscore on Xbox Gamepass is 'Townscaper', which can be completed in approximately 30 minutes!")]
[DataRow(2, "Which game on Xbox Gamepass has the highest completion rate?", "The game with the highest completion rate on Xbox Gamepass is 'The Walking Dead: Michonne' with a completion rate of 84.7%!")]
[DataRow(3, "What do gamers think of 'Forza Horizon 5'?", "Forza Horizon 5 currently has a rating of 4.5 out of 5 stars! Gamers love this game!")]
[DataRow(4, "How long does it typically take to complete 'Halo Infinite'?", "On average, it takes about 100-120 hours to complete all challenges of 'Halo Infinite'.")]
[DataRow(5, "I'm looking for a game that will take me a long time to finish, preferably an RPG. Any suggestions?", "'Black Desert' is a great choice for a long RPG experience, with an average completion time of 500-750 hours to complete all gamerscope challenges!")]
public async Task KnowledgebaseChatAgent_EvaluateQuestionAnswer_Scores(int questionId, string questionText, string expectedAnswer)
{
    var question = new EvalQuestion(questionId, questionText, expectedAnswer);
    using var scope = ServiceProvider!.CreateScope();
    var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
    var agent = KnowledgebaseChatAgent.BuildKnowledgebaseChatAgent(chatClient);

    await EvaluateQuestion(
        question,
        s_defaultReportingConfiguration!,
        agent,
        TestContext!.CancellationTokenSource.Token);
}
```

</details>

---

## Run and Validate

1. [ ] Build the solution to verify there are no compilation errors
1. [ ] Execute the test using the `dotnet test` CLI with a filter for your test class:

	```Powershell
	dotnet test tests/AgentEvalsWorkshop.Tests --filter "FullyQualifiedName~AgentRetrievalEvalTests"
	```

	@[Click this to run the unit tests][Run Unit Tests]{Powershell}

[Run Unit Tests]:
```Powershell
cd d:\github\seiggy\agent-unit-testing
dotnet test tests/AgentEvalsWorkshop.Tests --filter "FullyQualifiedName~AgentRetrievalEvalTests"
```

## Generate and View the Evaluation Report

1. [ ] After the tests complete, use the `aieval` CLI tool to generate an HTML report:

	```Powershell
	dotnet aieval report -p C:\TestReports -o retrieval-eval-report.html
	```

	@[Click this to run the test report][AIEval Report]{Powershell}

[AIEval Report]:
```Powershell
cd d:\github\seiggy\agent-unit-testing
dotnet aieval report -p C:\TestReports -o retrieval-eval-report.html
```

2. [ ] Open `retrieval-eval-report.html` in your browser to view the detailed evaluation results

**What you'll see in the report:**

- **Multiple Metrics Per Scenario**: Each question shows scores for Relevance, Coherence, and Groundedness
- **Aggregated Results**: Overall pass/fail rates across all data rows
- **Detailed Diagnostics**: For each evaluator, explanations of why scores were assigned
- **Conversation History**: The full exchange between user and agent

📚 **Documentation Links:**
- [aieval report tool](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting#generate-a-report)

---

## Success Criteria

A passing test suite indicates:
- ✅ The agent correctly retrieves information from the knowledge base
- ✅ Responses are relevant to the user's questions
- ✅ Responses are coherent and well-structured
- ✅ Responses are grounded in the actual CSV data
- ✅ All ratings are `Good` or `Exceptional`

---

## Troubleshooting

| Symptom | Possible Cause | Resolution |
|---------|----------------|------------|
| `FileNotFoundException` for CSV | Incorrect path or missing data file | Ensure `./Data/Gamepass_Games_v1.csv` exists relative to test output directory |
| Groundedness fails | Response contains fabricated information | Check that the agent is using the knowledge base tool correctly |
| `NullReferenceException` on ChatConfiguration | Assembly initialization failed | Ensure Aspire AppHost is running and connection string is valid |
| All evaluators return `Inconclusive` | LLM didn't respond in expected format | Check ChatConfiguration and model availability |
| Relevance score is low | Response doesn't address the question | Review agent instructions and tool usage |

---

## Next Steps

In the next exercise (US3), you will create a **custom evaluator** to compare the agent's responses against expected answers, giving you more control over domain-specific evaluation criteria.
