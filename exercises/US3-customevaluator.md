# Exercise: Creating a Custom Evaluator

## Goal
Create a **custom AnswerScoringEvaluator** that compares AI responses against expected answers, then integrate it into your existing test suite. You will learn how to build domain-specific evaluators using the `IEvaluator` interface and the LLM-as-Judge pattern.

## Learning Objectives
- Understand the `IEvaluator` interface and how to implement it
- Learn to create custom **EvaluationContext** classes for passing additional data
- Use the **LLM-as-Judge** pattern to evaluate AI responses
- Integrate custom evaluators with built-in evaluators in a test suite
- Work with structured output from LLMs using `GetResponseAsync<T>()`

## Prerequisites
- Exercise US2 completed successfully
- Aspire AppHost configured and running
- Solution open in Visual Studio 2026 or VS Code
- Azure Foundry endpoint configured in user secrets or environment variables

## Key Concepts

Custom evaluators allow you to define domain-specific evaluation criteria that go beyond the built-in evaluators. This exercise implements an **Answer Scoring Evaluator** that:

| Component | Purpose |
|-----------|---------|
| **IEvaluator Interface** | Standard contract for all evaluators |
| **EvaluationContext** | Passes additional data (expected answer) to the evaluator |
| **LLM-as-Judge** | Uses an LLM to compare responses and assign scores |
| **NumericMetric** | Returns a score from 1-5 with interpretation |

📚 **Documentation Links:**
- [Creating Custom Evaluators](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting#create-custom-evaluators)
- [IEvaluator Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.ievaluator?view=net-10.0-pp)
- [EvaluationContext Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.evaluationcontext?view=net-10.0-pp)

---

## Create the Evaluator File

1. [ ] Create a new file **AnswerScoringEvaluator.cs** in the **tests/AgentEvalsWorkshop.Tests/Helpers/** folder

1. [ ] Add the necessary **using** directives and place the class in the `Microsoft.Extensions.AI.Evaluation.Quality` namespace

> [!knowledge] Why this namespace?
> 
> Placing your custom evaluator in the same namespace as the built-in evaluators allows seamless integration and discoverability alongside `RelevanceEvaluator`, `CoherenceEvaluator`, etc.

<details>
<summary>💡 Show Namespace Setup</summary>

```csharp
// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

namespace Microsoft.Extensions.AI.Evaluation.Quality;
```

</details>

## Define the Evaluator Class

1. [ ] Create a `public sealed class` named `AnswerScoringEvaluator` that implements `IEvaluator`
1. [ ] Define a private constant string `MetricName` with value `"Answer Score"`
1. [ ] Create a public constant `AnswerScoreMetricName` that exposes the metric name
1. [ ] Implement the `EvaluationMetricNames` property that returns a collection containing your metric name

> [!knowledge] IEvaluator Interface
> 
> The `IEvaluator` interface requires:
> - `EvaluationMetricNames`: Declares which metrics this evaluator produces
> - `EvaluateAsync()`: The main evaluation logic

<details>
<summary>💡 Show Class Structure</summary>

```csharp
public sealed class AnswerScoringEvaluator : IEvaluator
{
    private const string MetricName = "Answer Score";

    public const string AnswerScoreMetricName = MetricName;

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    // EvaluateAsync will be added next
}
```

</details>

## Create the Context Class

1. [ ] Inside `AnswerScoringEvaluator`, create a `public sealed class` named `Context` that inherits from `EvaluationContext`
1. [ ] The constructor should accept a `string expectedAnswer` parameter
1. [ ] Call the base constructor with a context name and the expected answer as content
1. [ ] Expose the expected answer through a public property `ExpectedAnswer`

> [!knowledge] EvaluationContext
> 
> Custom context classes allow you to pass additional information to your evaluator. This is how we'll provide the expected answer for comparison.

<details>
<summary>💡 Show Context Implementation</summary>

```csharp
public sealed class Context(string expectedAnswer) : EvaluationContext(ContextName, content: expectedAnswer)
{
    private const string ContextName = "Answer Score";

    public string ExpectedAnswer { get; } = expectedAnswer;
}
```

</details>

## Implement the EvaluateAsync Method Signature

1. [ ] Add the `EvaluateAsync` method with the standard `IEvaluator` signature:
   - `IEnumerable<ChatMessage> messages`
   - `ChatResponse modelResponse`
   - `ChatConfiguration? chatConfiguration`
   - `IEnumerable<EvaluationContext>? additionalContext`
   - `CancellationToken cancellationToken`

1. [ ] Validate that `modelResponse` and `chatConfiguration` are not null using `ArgumentNullException.ThrowIfNull()`

1. [ ] Create a `NumericMetric` with your metric name and wrap it in an `EvaluationResult`

> [+hint] 📚 **Documentation Links:**
>
> [EvaluationResult Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.evaluation.evaluationresult?view=net-10.0-pp)

<details>
<summary>💡 Show Method Signature</summary>

```csharp
public async ValueTask<EvaluationResult> EvaluateAsync(
    IEnumerable<ChatMessage> messages,
    ChatResponse modelResponse,
    ChatConfiguration? chatConfiguration = null,
    IEnumerable<EvaluationContext>? additionalContext = null,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(modelResponse);
    ArgumentNullException.ThrowIfNull(chatConfiguration);

    var numericMetric = new NumericMetric(MetricName);
    var result = new EvaluationResult(numericMetric);

    // Validation and evaluation logic will be added next...
}
```

</details>

## Add Input Validation

1. [ ] Extract the user request from messages using the `TryGetUserRequest()` extension method
1. [ ] If extraction fails, add an error diagnostic and return early
1. [ ] Validate the model response text is not null or whitespace
1. [ ] If validation fails, add an error diagnostic and return early

> [!knowledge] Defensive Evaluation
> 
> Robust evaluators validate their inputs and provide clear error diagnostics when something is wrong. This helps with debugging test failures.

<details>
<summary>💡 Show Validation Logic</summary>

```csharp
if (!messages.TryGetUserRequest(out ChatMessage? userRequest, out IReadOnlyList<ChatMessage> conversationHistory))
{
    result.AddDiagnosticsToAllMetrics(
        EvaluationDiagnostic.Error(
            $"The ${messages} supplied for evaluation did not contain a user request as the last message."));
    return result;
}

if (string.IsNullOrWhiteSpace(modelResponse.Text))
{
    result.AddDiagnosticsToAllMetrics(
        EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));
    return result;
}
```

</details>

## Create the Evaluation Prompt Method

1. [ ] Create a `private static` method named `GetEvaluationInstructions` that returns `List<ChatMessage>`
1. [ ] Accept parameters for: `userRequest`, `modelResponse`, `includedHistory`, and `additionalContext`
1. [ ] Render the model response and user request as text
1. [ ] Extract the expected answer from the `Context` in `additionalContext`
1. [ ] If the context is missing, throw an `InvalidOperationException`

<details>
<summary>💡 Show Method Start</summary>

```csharp
private static List<ChatMessage> GetEvaluationInstructions(
    ChatMessage? userRequest,
    ChatResponse modelResponse,
    IEnumerable<ChatMessage> includedHistory,
    IEnumerable<EvaluationContext>? additionalContext)
{
    string renderedModelResponse = modelResponse.RenderText();
    string renderedUserRequest = userRequest?.RenderText() ?? string.Empty;
    string answer;

    if (additionalContext is not null &&
        additionalContext.OfType<Context>().FirstOrDefault() is Context context)
    {
        answer = context.ExpectedAnswer;
    }
    else
    {
        throw new InvalidOperationException($"The ExpectedAnswer must be provided in the additional context.");
    }

    // Build the prompt next...
}
```

</details>

## Build the LLM-as-Judge Prompt

1. [ ] Create a prompt that:
   - Describes the AI assistant's domain (Xbox Gamepass library)
   - Presents the question, expected truth, and assistant's answer
   - Defines the JSON output format with scores and descriptions
   - Specifies scoring criteria (1-5 scale)
1. [ ] Return the prompt wrapped in a `ChatMessage` with `ChatRole.User`

> [!knowledge] LLM-as-Judge Pattern
> 
> This pattern uses another LLM to evaluate responses. The key is crafting a clear prompt that defines:
> - The evaluation criteria
> - The expected output format
> - The scoring scale and what each score means

<details>
<summary>💡 Show Complete Prompt</summary>

```csharp
var prompt = $$"""
There is an AI assistant that answers questions about games in the Xbox Gamepass library. The questions
may relate to game completion rate, playtime, rating, and achievement score details.

You are evaluating the quality of an AI assistant's response to several questions. Here are the
questions, the desired true answers, and the answers given by the AI system:

<questions>
    <question index="0">
        <text>{{renderedUserRequest}}</text>
        <truth>{{answer}}</truth>
        <assistantAnswer>{{renderedModelResponse}}</assistantAnswer>
    </question>
</questions>

Evaluate each of the assistant's answers separately by replying in this JSON format:

{
    "scores": [
        { "index": 0, "descriptionOfQuality": string, "scoreLabel": number },
        { "index": 1, "descriptionOfQuality": string, "scoreLabel": number },
        ... etc ...
    ]
]

Score only based on whether the assistant's answer is true and answers the question. As long as the
answer covers the question and is consistent with the truth, it should score as perfect. There is
no penalty for giving extra on-topic information or advice. Only penalize for missing necessary facts
or being misleading, or providing incorrect information that has no basis in the knowledgebase.

The descriptionOfQuality should be up to 5 words summarizing to what extent the assistant answer
is correct and sufficient.

Based on descriptionOfQuality, the scoreLabel must be a number between 1 and 5 inclusive, where 5 is best and 1 is worst.
Do not use any other words for scoreLabel. You may only pick one of those scores.

"""
;

return [new ChatMessage(ChatRole.User, prompt)];
```

</details>

## Add Response Record Types

1. [ ] Outside the `AnswerScoringEvaluator` class (but in the same file), add record types for structured output parsing:

```csharp
record ScoringResponse(AnswerScore[] Scores);
record AnswerScore(int Index, int ScoreLabel, string DescriptionOfQuality);
```

> [!knowledge] Structured Output
> 
> Using records with `GetResponseAsync<T>()` enables the LLM to return structured data that can be easily parsed and processed.

## Complete the Evaluation Logic

1. [ ] In `EvaluateAsync`, call `GetEvaluationInstructions()` to build the prompt
1. [ ] Use `chatConfiguration.ChatClient.GetResponseAsync<ScoringResponse>()` to get structured output
1. [ ] Handle cases where the response is invalid or contains no scores
1. [ ] Extract the score and set `numericMetric.Value`
1. [ ] Add the quality description as an informational diagnostic

<details>
<summary>💡 Show Evaluation Logic</summary>

```csharp
var evaluationInstructions = GetEvaluationInstructions(
    userRequest,
    modelResponse,
    conversationHistory,
    additionalContext);

var response = await chatConfiguration.ChatClient.GetResponseAsync<ScoringResponse>(
    evaluationInstructions,
    cancellationToken: cancellationToken);

if (!response.TryGetResult(out var scoringResponse))
{
    result.AddDiagnosticsToAllMetrics(
        EvaluationDiagnostic.Error("Scoring response was not provided in a valid format."));
    return result;
}

if (scoringResponse.Scores is not [var score, ..])
{
    result.AddDiagnosticsToAllMetrics(
        EvaluationDiagnostic.Error("Scoring response contained no scores."));
    return result;
}

numericMetric.Value = score.ScoreLabel;

if (!string.IsNullOrWhiteSpace(score.DescriptionOfQuality))
{
    numericMetric.AddDiagnostics(EvaluationDiagnostic.Informational(score.DescriptionOfQuality));
}
```

</details>

## Add the Interpret Helper Method

1. [ ] Create an `internal static` method named `Interpret` that takes a `NumericMetric` and returns `EvaluationMetricInterpretation`
1. [ ] Map scores 1-5 to appropriate `EvaluationRating` values:
   - 1 → `Unacceptable`
   - 2 → `Poor`
   - 3 → `Average`
   - 4 → `Good`
   - 5 → `Exceptional`
1. [ ] Return `Inconclusive` for unexpected scores and mark as failed

<details>
<summary>💡 Show Interpret Implementation</summary>

```csharp
internal static EvaluationMetricInterpretation Interpret(NumericMetric metric)
{
    double score = metric?.Value ?? -1.0;
    EvaluationRating rating = score switch {
        1.0 => EvaluationRating.Unacceptable,
        2.0 => EvaluationRating.Poor,
        3.0 => EvaluationRating.Average,
        4.0 => EvaluationRating.Good,
        5.0 => EvaluationRating.Exceptional,
        _ => EvaluationRating.Inconclusive,
    };
    return new EvaluationMetricInterpretation(rating, failed: rating == EvaluationRating.Inconclusive);
}
```

</details>

## Set the Interpretation and Return

1. [ ] At the end of `EvaluateAsync`, call `Interpret()` and assign to `numericMetric.Interpretation`
1. [ ] Return the result

```csharp
numericMetric.Interpretation = Interpret(numericMetric);
return result;
```

---

# Part 2: Integrate the Custom Evaluator into Tests

## Update the GetEvaluators Method

1. [ ] Open your **AgentRetrievalEvalTests.cs** file from US2
1. [ ] Add the `AnswerScoringEvaluator` to your evaluators collection

<details>
<summary>💡 Show Updated GetEvaluators</summary>

```csharp
private static IEnumerable<IEvaluator> GetEvaluators()
{
    var relevanceEvaluator = new RelevanceEvaluator();
    var coherenceEvaluator = new CoherenceEvaluator();
    var groundednessEvaluator = new GroundednessEvaluator();
    var answerScoringEvaluator = new AnswerScoringEvaluator();
    return [ relevanceEvaluator, coherenceEvaluator, groundednessEvaluator, answerScoringEvaluator ];
}
```

</details>

## Update the EvaluateQuestion Method

1. [ ] Add the `AnswerScoringEvaluator.Context` to the `additionalContext` array in `EvaluateAsync()`
1. [ ] Pass the expected answer from the `EvalQuestion` record

<details>
<summary>💡 Show Updated EvaluateAsync Call</summary>

```csharp
var result = await scenario.EvaluateAsync(
    messages: [new ChatMessage(ChatRole.User, question.Question)],
    modelResponse: response.ToChatResponse(),
    additionalContext: [new AnswerScoringEvaluator.Context(question.Answer),
        new GroundednessEvaluatorContext(await GetKnowledgebaseContext())],
    cancellationToken: cancellationToken
);
```

</details>

## Update the Validate Method

1. [ ] Add assertions for the new `AnswerScoringEvaluator` metric
1. [ ] Retrieve the score using `AnswerScoringEvaluator.AnswerScoreMetricName`
1. [ ] Assert the rating is `Good` or `Exceptional` and not failed

<details>
<summary>💡 Show Updated Validate Method</summary>

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
    
    // Retrieve the score for answer quality from the EvaluationResult.
    NumericMetric answerScore = result.Get<NumericMetric>(AnswerScoringEvaluator.AnswerScoreMetricName);
    Assert.IsTrue(answerScore.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
    Assert.IsFalse(answerScore.Interpretation?.Failed, answerScore.Reason);
}
```

</details>

---

## Run and Validate

1. [ ] Build the solution to verify there are no compilation errors
1. [ ] Execute the test using the `dotnet test` CLI:

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

1. [ ] After the tests complete, generate an HTML report:

	```Powershell
	dotnet aieval report -p C:\TestReports -o custom-eval-report.html
	```

	@[Click this to run the test report][AIEval Report]{Powershell}

[AIEval Report]:
```Powershell
cd d:\github\seiggy\agent-unit-testing
dotnet aieval report -p C:\TestReports -o custom-eval-report.html
```

2. [ ] Open `custom-eval-report.html` in your browser

**What you'll see in the report:**

- **Answer Score Metric**: Your custom evaluator's scores alongside the built-in evaluators
- **Quality Descriptions**: The 5-word summaries from your evaluator's diagnostics
- **Four Metrics Per Question**: Relevance, Coherence, Groundedness, and Answer Score

📚 **Documentation Links:**
- [aieval report tool](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/evaluate-with-reporting#generate-a-report)

---

## Success Criteria

A passing test suite indicates:
- ✅ Your custom evaluator correctly implements the `IEvaluator` interface
- ✅ The evaluator receives and uses the expected answer context
- ✅ LLM-as-Judge scoring produces meaningful results
- ✅ Scores are correctly mapped to `EvaluationRating` values
- ✅ All four evaluators work together in the test suite
- ✅ All ratings are `Good` or `Exceptional`

---

## Troubleshooting

| Symptom | Possible Cause | Resolution |
|---------|----------------|------------|
| `InvalidOperationException` for ExpectedAnswer | Missing context | Verify `AnswerScoringEvaluator.Context` is passed in `additionalContext` |
| `TryGetResult` returns false | LLM response doesn't match schema | Check that the prompt clearly specifies the JSON format |
| Score is always `Inconclusive` | Score is outside 1-5 range | Review the LLM's raw response for unexpected values |
| Answer Score is low but response looks correct | Expected answer too specific | Make expected answers focus on key facts, not exact wording |
| `NullReferenceException` on metric | Evaluator not registered | Ensure `AnswerScoringEvaluator` is in `GetEvaluators()` return |

---

## Extension Challenges

Once you've completed the exercise, try these extensions:

1. **Add Detailed Diagnostics**: Include the full reasoning from the LLM in the diagnostic output

2. **Partial Credit Scoring**: Modify the prompt to give partial credit for partially correct answers

3. **Multi-Aspect Scoring**: Create an evaluator that scores multiple aspects (accuracy, completeness, formatting) separately

4. **Confidence Scoring**: Have the LLM also report a confidence level for its score

5. **Custom Thresholds**: Add constructor parameters to customize what score constitutes `Good` vs `Exceptional`