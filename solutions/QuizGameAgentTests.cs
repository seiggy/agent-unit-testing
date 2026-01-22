#pragma warning disable AIEVAL001
using AgentEvalsWorkshop.Agents;
using AgentEvalsWorkshop.Tests.Helpers;
using Azure;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class QuizGameAgentTests : BaseIntegrationTest
{
    private static ReportingConfiguration? s_defaultReportingConfiguration;
    private const string startingInstructions = """
        ## Quiz Game Agent — STRICT SYSTEM PROMPT

        ### Role
        You are a **Quiz Game Agent** that runs a complete trivia game from start to finish. You must follow the workflow exactly as written. **Any missing, skipped, or out-of-order step is a failure.**

        ---

        ## GLOBAL HARD RULES (Never Break These)
        1. **Exactly 10 Questions Per Game** — no more, no fewer.
        2. **ALL quiz state must come from tools** — questions, answers, scoring, progression, and scoreboard.
        3. **NEVER invent, paraphrase, or guess** questions, answers, or scores.
        4. **Every required tool call must actually be made** — do not simulate results.
        5. **Tool calls must occur immediately when specified.** Do not wait for another user message.

        ---

        ## GAME START DETECTION (CRITICAL)
        Trigger a new game **ONLY** if the user explicitly requests to start a trivia/quiz game (e.g., "Let's play", "Start a trivia game", "Quiz me").

        ### If a start request is detected:
        1. **IGNORE any answers included in the same message** (e.g., "My answer is Paris").
        2. **Do NOT ask clarifying questions.**
        3. **Immediately proceed to STEP 1 below in the SAME TURN.**

        ---

        ## STEP 1 — CREATE QUESTIONS (MANDATORY TOOL CALL)
        Immediately call:

        **CreateQuizQuestions**
        - Generate **exactly 10** question–answer pairs
        - If the user specifies a category, **ALL 10 questions MUST be from that category**

        ❌ Do NOT present any question text before this tool is called.

        ---

        ## STEP 2 — ASK QUESTION 1 (MANDATORY, SAME TURN)
        Immediately after CreateQuizQuestions completes:
        1. Call **GetCurrentQuestion**
        2. Display the returned question to the user

        ✅ This MUST happen in the SAME response turn as CreateQuizQuestions
        ❌ Never manually write a question

        ---

        ## STEP 3 — HANDLE USER ANSWERS
        When the user submits an answer:
        1. Compare it to the correct answer (allow typos, capitalization differences, and common synonyms)
        2. Call **ScoreQuestion** with `correct = true` or `false`
        3. Explicitly tell the user whether they were correct or incorrect

        ❌ Do NOT move forward before scoring

        ---

        ## STEP 4 — ADVANCE THE GAME
        After scoring:
        1. Call **MoveToNextQuestion**
        2. If questions remain:
           - Call **GetCurrentQuestion**
           - Present the next question

        ✅ Repeat STEPS 3–4 until all 10 questions are answered

        ---

        ## STEP 5 — END GAME & SCOREBOARD
        After the 10th question is scored:
        1. Call **GenerateScoreboard**
        2. Display the returned **HTML scoreboard exactly as provided**
        3. End the game

        ❌ Do NOT ask any further questions

        ---

        ## BEHAVIOR CONSTRAINTS
        - Friendly, concise trivia-host tone
        - Never assume an answer
        - Never skip or reorder steps
        - If unsure, follow the step order — **tool calls take priority over conversation**

        ---

        ## REQUIRED FLOW SUMMARY
        Start request → CreateQuizQuestions → GetCurrentQuestion → ScoreQuestion → MoveToNextQuestion → GetCurrentQuestion → … → GenerateScoreboard

        **Any deviation from this flow is incorrect behavior.**
        """;

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

    [TestInitialize]
    public void TestInitialize()
    {
        // Clear any existing quiz sessions before each test
        QuizGameAgent.ClearAllSessions();
    }

    private static IEnumerable<IEvaluator> GetEvaluators()
    {
        var taskAdherenceEvaluator = new TaskAdherenceEvaluator();
        var quizGameRulesEvaluator = new QuizGameRulesEvaluator();
        return [taskAdherenceEvaluator, quizGameRulesEvaluator];
    }

    /// <summary>
    /// Validates that the agent creates 10 trivia questions when asked to start a game.
    /// Rule: When a user asks to start a game, generate 10 questions and answers, Trivia style.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    public async Task QuizAgent_WhenAskedToStartGame_ShouldCreateTenQuestions()
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_StartGame", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());

        // Act
        var userMessage = "Let's play a trivia game! Start a new game for me.";
        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage> { new ChatMessage(ChatRole.User, userMessage) };

        var response = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Evaluate
        var result = await scenario.EvaluateAsync(
            messages: chatHistory,
            modelResponse: response.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Assert
        ValidateTaskAdherence(result);
        
        // Additional assertions: Check that the response indicates questions were created
        var responseText = response.ToChatResponse().Text ?? "";
        Assert.IsTrue(
            responseText.Contains("question", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("quiz", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("trivia", StringComparison.OrdinalIgnoreCase),
            $"Agent should acknowledge starting a quiz game. Response was: {responseText}");
    }

    /// <summary>
    /// Validates that the agent generates category-specific questions when requested.
    /// Rule: If the user asks for a specific category, generate questions from that category.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    [DataRow("Computer Science")]
    [DataRow("Video Games")]
    [DataRow("Sports")]
    [DataRow("Movies")]
    public async Task QuizAgent_WhenAskedForCategory_ShouldGenerateCategoryQuestions(string category)
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_{category}", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());

        // Act
        var userMessage = $"Start a trivia game with questions about {category}.";
        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage> { new ChatMessage(ChatRole.User, userMessage) };

        var response = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Evaluate
        var result = await scenario.EvaluateAsync(
            messages: chatHistory,
            modelResponse: response.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Assert
        ValidateTaskAdherence(result);
    }

    /// <summary>
    /// Validates that the agent asks the first question after creating the quiz.
    /// Rule: Start the game by asking the user the first question in your generated collection.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    public async Task QuizAgent_AfterStartingGame_ShouldAskFirstQuestion()
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_FirstQuestion", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());

        // Act
        var userMessage = "Start a new trivia game!";
        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage> { new ChatMessage(ChatRole.User, userMessage) };

        var response = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Evaluate
        var result = await scenario.EvaluateAsync(
            messages: chatHistory,
            modelResponse: response.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Assert
        ValidateTaskAdherence(result);
    }

    /// <summary>
    /// Validates that the agent correctly scores a user's answer.
    /// Rule: Score the user's answer based on comparing it to the stored answer.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    public async Task QuizAgent_WhenUserAnswers_ShouldScoreCorrectly()
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_ScoreAnswer", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());
        var thread = await agent.GetNewThreadAsync();

        // Start the game first
        var startMessage = "Start a new trivia game!";
        var chatHistory = new List<ChatMessage> { new ChatMessage(ChatRole.User, startMessage) };
        
        var startResponse = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );
        chatHistory.AddRange(startResponse.Messages);


        // Act - Answer a question (we'll give a plausible answer)
        var answerMessage = "My answer is: Paris";
        chatHistory.Add(new ChatMessage(ChatRole.User, answerMessage));
        
        var answerResponse = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Evaluate
        var result = await scenario.EvaluateAsync(
            messages: chatHistory.Where(m => m.Role != ChatRole.Tool),
            modelResponse: answerResponse.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Assert
        ValidateTaskAdherence(result);
        
        // Response should indicate whether answer was correct or incorrect
        var responseText = answerResponse.ToChatResponse().Text ?? "";
        Assert.IsTrue(
            responseText.Contains("correct", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("incorrect", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("right", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("wrong", StringComparison.OrdinalIgnoreCase),
            $"Agent should score the answer as correct or incorrect. Response was: {responseText}");
    }

    /// <summary>
    /// Validates that the agent generates an HTML scoreboard after all questions are answered.
    /// Rule: When the user finishes all 10 questions, generate a score board report in HTML.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    public async Task QuizAgent_WhenQuizComplete_ShouldGenerateHtmlScoreboard()
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_Scoreboard", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());
        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage>();

        // Start the game
        chatHistory.Add(new ChatMessage(ChatRole.User, "Start a trivia game!"));
        var response = await agent.RunAsync(chatHistory, thread: thread, cancellationToken: TestContext.CancellationTokenSource.Token);
        chatHistory.AddRange(response.Messages);

        // Answer questions (simulate completing the quiz)
        for (int i = 0; i < 10; i++)
        {
            var question = response.ToChatResponse().Text;
            var answer = await chatClient.GetResponseAsync(new ChatMessage(ChatRole.User, question));
            chatHistory.Add(new ChatMessage(ChatRole.User, answer.Text));
            response = await agent.RunAsync(chatHistory, thread: thread, cancellationToken: TestContext.CancellationTokenSource.Token);
            chatHistory.AddRange(response.Messages);
        }

        // Ask for final results
        chatHistory.Add(new ChatMessage(ChatRole.User, "Show me my final score and results!"));
        var finalResponse = await agent.RunAsync(chatHistory, thread: thread, cancellationToken: TestContext.CancellationTokenSource.Token);

        // Evaluate
        var result = await scenario.EvaluateAsync(
            messages: chatHistory.Where(m => m.Role != ChatRole.Tool),
            modelResponse: finalResponse.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Assert
        ValidateTaskAdherence(result);
        
        // Response should contain HTML or scoreboard elements
        var responseText = finalResponse.ToChatResponse().Text ?? "";
        Assert.IsTrue(
            responseText.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("score", StringComparison.OrdinalIgnoreCase) ||
            responseText.Contains("result", StringComparison.OrdinalIgnoreCase),
            $"Agent should generate a scoreboard. Response was: {responseText}");
    }

    /// <summary>
    /// Validates a complete game flow from start to finish.
    /// This is an end-to-end test of all game rules.
    /// </summary>
    [TestMethod]
    [TestCategory("AgentValidation")]
    public async Task QuizAgent_CompleteGameFlow_ShouldFollowAllRules()
    {
        // Arrange
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_FullGame", cancellationToken: TestContext!.CancellationTokenSource.Token);

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);

        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());
        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage>();

        // Step 1: Start a game with a specific category
        chatHistory.Add(new ChatMessage(ChatRole.User, "Let's play a Science trivia game!"));
        var startResponse = await agent.RunAsync(chatHistory, thread: thread, cancellationToken: TestContext.CancellationTokenSource.Token);
        chatHistory.AddRange(startResponse.Messages);

        var startText = startResponse.ToChatResponse().Text ?? "";
        
        // Validate: Should create questions AND ask the first question
        Assert.IsTrue(
            startText.Contains("?") || startText.Contains("Question", StringComparison.OrdinalIgnoreCase),
            $"Agent should ask the first question immediately. Response was: {startText}");

        // Step 2: Answer the first question
        var userAnswer = new ChatMessage(ChatRole.User, "The answer is: Hydrogen");
        chatHistory.Add(userAnswer);
        var answerResponse = await agent.RunAsync(chatHistory, thread: thread, cancellationToken: TestContext.CancellationTokenSource.Token);

        // the evaluator only wants the chat history up until the last user message
        //chatHistory.AddRange(answerResponse.Messages.Where(m => m.Role != ChatRole.Tool));

        var answerText = answerResponse.ToChatResponse().Text ?? "";
        
        // Validate: Should judge the answer AND ask the next question
        Assert.IsTrue(
            answerText.Contains("correct", StringComparison.OrdinalIgnoreCase) ||
            answerText.Contains("incorrect", StringComparison.OrdinalIgnoreCase),
            $"Agent should indicate if answer was correct. Response was: {answerText}");

        // Evaluate the complete flow
        var result = await scenario.EvaluateAsync(
            messages: chatHistory,
            modelResponse: answerResponse.ToChatResponse(),
            additionalContext: [toolContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        ValidateTaskAdherence(result);
        ValidateQuizGameRules(result);
    }

    private static void ValidateTaskAdherence(EvaluationResult result)
    {
        NumericMetric taskAdherence = result.Get<NumericMetric>(TaskAdherenceEvaluator.TaskAdherenceMetricName);
        Assert.IsFalse(taskAdherence.Interpretation?.Failed, $"Task adherence failed: {taskAdherence.Reason}");
        Assert.IsTrue(
            taskAdherence.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional,
            $"Task adherence should be Good or Exceptional. Rating: {taskAdherence.Interpretation?.Rating}, Reason: {taskAdherence.Reason}");
    }

    private static void ValidateQuizGameRules(EvaluationResult result)
    {
        NumericMetric rulesAdherence = result.Get<NumericMetric>(QuizGameRulesEvaluator.QuizGameRulesMetricName);
        Assert.IsFalse(rulesAdherence.Interpretation?.Failed, $"Quiz game rules adherence failed: {rulesAdherence.Reason}");
        Assert.IsTrue(
            rulesAdherence.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional,
            $"Quiz game rules adherence should be Good or Exceptional. Rating: {rulesAdherence.Interpretation?.Rating}");
    }

    /// <summary>
    /// This test runs all game scenarios, collects failures, and generates an improved prompt.
    /// The suggested improved prompt is output to the test results for iterative prompt engineering.
    /// </summary>
    [TestMethod]
    [TestCategory("PromptImprovement")]
    public async Task ImproveInstructions_SuggestsPromptChanges()
    {
        var testResults = new List<TestEvaluationResult>();

        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();

        // Run multiple test scenarios and collect results
        testResults.Add(await RunScenarioForImprovement(chatClient, "StartGame", "Let's play a trivia game! Start a new game for me."));
        testResults.Add(await RunScenarioForImprovement(chatClient, "CategoryGame", "Start a trivia game about Video Games."));
        testResults.Add(await RunScenarioForImprovement(chatClient, "AnswerQuestion", "Start a game and then answer: Paris"));
        testResults.Add(await RunScenarioForImprovement(chatClient, "CompleteFlow", "Let's play Science trivia! My answer is Hydrogen."));

        // Generate improved prompt
        var generator = new PromptImprovementGenerator(chatClient);
        var improvementResult = await generator.GenerateImprovedPromptAsync(
            startingInstructions,
            testResults,
            QuizGameRulesEvaluator.Context.GameRules,
            TestContext!.CancellationTokenSource.Token
        );

        // Output results to test console
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("                    PROMPT IMPROVEMENT ANALYSIS                                 ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📋 CURRENT INSTRUCTIONS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine(startingInstructions);
        Console.WriteLine();
        Console.WriteLine("📊 TEST RESULTS SUMMARY:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        
        foreach (var testResult in testResults)
        {
            var status = testResult.Passed ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"  {status} - {testResult.TestName}");
            if (testResult.Violations.Any())
            {
                foreach (var violation in testResult.Violations)
                {
                    Console.WriteLine($"         └── {violation}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("💡 TARGETED FIXES:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        foreach (var fix in improvementResult.TargetedFixes)
        {
            Console.WriteLine($"  Issue: {fix.Issue}");
            Console.WriteLine($"  Fix:   {fix.Fix}");
            Console.WriteLine();
        }

        Console.WriteLine("📝 EXPLANATION:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine(improvementResult.Explanation);
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("                    SUGGESTED IMPROVED PROMPT                                   ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine(improvementResult.ImprovedPrompt);
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("Copy the improved prompt above and update the 'startingInstructions' constant.");
        Console.WriteLine("Then run the tests again to validate the improvement!");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");

        // This test always "passes" - it's a diagnostic tool, not a strict assertion
        // The value is in the output, which guides prompt improvement
        Assert.IsTrue(improvementResult.Success, "Failed to generate improved prompt.");
    }

    private async Task<TestEvaluationResult> RunScenarioForImprovement(
        IChatClient chatClient, 
        string testName, 
        string userMessage)
    {
        QuizGameAgent.ClearAllSessions();
        
        await using ScenarioRun scenario = await s_defaultReportingConfiguration!
            .CreateScenarioRunAsync($"{ScenarioName}_{testName}_Improvement", 
                cancellationToken: TestContext!.CancellationTokenSource.Token);

        var agent = QuizGameAgent.BuildQuizGameAgent(chatClient, startingInstructions);
        var toolContext = new TaskAdherenceEvaluatorContext(toolDefinitions: QuizGameAgent.GetToolDefinitions());
        var rulesContext = new QuizGameRulesEvaluator.Context(startingInstructions);

        var thread = await agent.GetNewThreadAsync();
        var chatHistory = new List<ChatMessage> { new ChatMessage(ChatRole.User, userMessage) };

        var response = await agent.RunAsync(
            chatHistory,
            thread: thread,
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        var result = await scenario.EvaluateAsync(
            messages: response.Messages,
            modelResponse: response.ToChatResponse(),
            additionalContext: [toolContext, rulesContext],
            cancellationToken: TestContext.CancellationTokenSource.Token
        );

        // Extract violations from the rules evaluator
        var violations = new List<string>();
        
        if (result.TryGet<StringMetric>(QuizGameRulesEvaluator.QuizGameRuleViolationsMetricName, out var violationsMetric) 
            && violationsMetric?.Value != null 
            && violationsMetric.Value != "No violations detected.")
        {
            violations.AddRange(violationsMetric.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        // Check if evaluation passed
        var passed = false;
        string? failureReason = null;
        
        if (result.TryGet<NumericMetric>(QuizGameRulesEvaluator.QuizGameRulesMetricName, out var rulesMetric))
        {
            passed = rulesMetric?.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional;
            failureReason = passed ? null : rulesMetric?.Reason;
        }

        return new TestEvaluationResult(
            TestName: testName,
            Passed: passed,
            UserInput: userMessage,
            AgentResponse: response.ToChatResponse().Text ?? "(no response)",
            Violations: violations,
            FailureReason: failureReason
        );
    }
}
