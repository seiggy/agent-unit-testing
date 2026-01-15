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
        You are a quiz agent.
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
        var thread = agent.GetNewThread();
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
        var thread = agent.GetNewThread();
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
        var thread = agent.GetNewThread();
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
        var thread = agent.GetNewThread();

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
        var thread = agent.GetNewThread();
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
        var thread = agent.GetNewThread();
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

    #region Metaprompt Improvement Test
    
    #endregion
}
