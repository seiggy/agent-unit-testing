using Aspire.Hosting;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using System.Net;
using YamlDotNet.Serialization;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class AgentRetrievalEvalTests
{
    private static DistributedApplication? s_appHost;
    private static ChatConfiguration? s_chatConfiguration;

    private static ReportingConfiguration? s_defaultReportingConfiguration;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public TestContext? TestContext { get; set; }

    private string ScenarioName => $"{TestContext!.FullyQualifiedTestClassName}.{TestContext.TestName}";
    private static string ExecutionName => $"{DateTime.Now:yyyyMMddTHHmmss}";
    const string SystemPrompt = 
        """
        You're an AI assistant that can answer questions related to astronomy.
        Keep your responses concise and under 100 words.
        Use the imperial measurement system for all measurements in your response.
        """;
    private static ChatResponse s_response = new();

    private static IEnumerable<IEvaluator> GetEvaluators()
    {
        var relevanceEvaluator = new RelevanceEvaluator();
        var coherenceEvaluator = new CoherenceEvaluator();
        var wordCountEvaluator = new WordCountEvaluator();
        return [ relevanceEvaluator, coherenceEvaluator, wordCountEvaluator ];
    }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AgentEvalsWorkshop_AppHost>();
        
        s_appHost = await appHost.BuildAsync();

        await s_appHost.ResourceNotifications
            .WaitForResourceAsync("chat", KnownResourceStates.Running, context.CancellationTokenSource.Token);
        await s_appHost.ResourceNotifications.WaitForResourceHealthyAsync(
            "agents", context.CancellationTokenSource.Token)
            .WaitAsync(DefaultTimeout, context.CancellationTokenSource.Token);

        var chatConnectionString = await s_appHost.GetConnectionStringAsync("chat", context.CancellationTokenSource.Token);
        
        if (string.IsNullOrEmpty(chatConnectionString))
        {
            throw new InvalidOperationException("Run aspire app host first, and ensure the chat deployment is available.");
        }

        s_chatConfiguration = GetAzureOpenAIChatConfiguration(chatConnectionString);

        s_defaultReportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: "C:\\TestReports",
            evaluators: GetEvaluators(),
            chatConfiguration: s_chatConfiguration,
            enableResponseCaching: true,
            executionName: ExecutionName
        );
    }

    private static ChatConfiguration GetAzureOpenAIChatConfiguration(string connectionString)
    {
        var chatConfiguration = new FoundryConnectionStringParts();
        chatConfiguration.ParseConnectionString(connectionString);

        var credential = chatConfiguration.TokenCredential?? new DefaultAzureCredential();
        var options = new AzureAIInferenceClientOptions();

        BearerTokenAuthenticationPolicy tokenPolicy = new(credential, ["https://cognitiveservices.azure.com/.default"]);
        options.AddPolicy(tokenPolicy, HttpPipelinePosition.PerRetry);

        IChatClient azureClient = new ChatCompletionsClient(
            chatConfiguration.Endpoint,
            credential,
            options
        ).AsIChatClient(chatConfiguration.Deployment);

        return new ChatConfiguration(azureClient);
    }

    private static async Task<(IList<ChatMessage> Messages, ChatResponse ModelResponse)> GetAstronomyConversationAsync(
        IChatClient chatClient,
        string astronomyQuestion,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, SystemPrompt),
            new ChatMessage(ChatRole.User, astronomyQuestion)
        };

        var chatOptions = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Text
        };

        var response = await chatClient.GetResponseAsync(
            messages,
            chatOptions,
            cancellationToken: cancellationToken);
        return (messages, response);
    }

    private static void Validate(EvaluationResult result)
    {
        // Retrieve the score for relevance from the <see cref="EvaluationResult"/>.
        NumericMetric relevance =
            result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        Assert.IsFalse(relevance.Interpretation!.Failed, relevance.Reason);
        Assert.IsTrue(relevance.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

        // Retrieve the score for coherence from the <see cref="EvaluationResult"/>.
        NumericMetric coherence =
            result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
        Assert.IsFalse(coherence.Interpretation!.Failed, coherence.Reason);
        Assert.IsTrue(coherence.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

        // Retrieve the word count from the <see cref="EvaluationResult"/>.
        NumericMetric wordCount = result.Get<NumericMetric>(WordCountEvaluator.WordCountMetricName);
        Assert.IsFalse(wordCount.Interpretation!.Failed, wordCount.Reason);
        Assert.IsTrue(wordCount.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
        Assert.IsFalse(wordCount.ContainsDiagnostics());
        Assert.IsTrue(wordCount.Value > 5 && wordCount.Value <= 100);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (s_appHost != null)
        {
            await s_appHost.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task SampleAndEvaluateResponse()
    {
        // Create a <see cref="ScenarioRun"/> with the scenario name
        // set to the fully qualified name of the current test method.
        await using ScenarioRun scenarioRun =
            await s_defaultReportingConfiguration!.CreateScenarioRunAsync(
                ScenarioName,
                additionalTags: ["Moon"]);

        // Use the <see cref="IChatClient"/> that's included in the
        // <see cref="ScenarioRun.ChatConfiguration"/> to get the LLM response.
        (IList<ChatMessage> messages, ChatResponse modelResponse) = await GetAstronomyConversationAsync(
            chatClient: scenarioRun.ChatConfiguration!.ChatClient,
            astronomyQuestion: "How far is the Moon from the Earth at its closest and furthest points?");

        // Run the evaluators configured in <see cref="s_defaultReportingConfiguration"/> against the response.
        EvaluationResult result = await scenarioRun.EvaluateAsync(messages, modelResponse);

        // Run some basic validation on the evaluation result.
        Validate(result);
    }
}
