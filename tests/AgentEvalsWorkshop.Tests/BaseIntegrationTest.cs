using Aspire.Hosting;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace AgentEvalsWorkshop.Tests
{
    [TestClass]
    public class BaseIntegrationTest
    {
        internal static DistributedApplication? s_appHost;
        private static ChatConfiguration? s_chatConfiguration;

        public static ChatConfiguration ChatConfiguration => s_chatConfiguration!;

        public TestContext? TestContext { get; set; }

        internal string ScenarioName => $"{TestContext!.FullyQualifiedTestClassName}.{TestContext.TestName}";
        internal static string ExecutionName => $"{DateTime.Now:yyyyMMddTHHmmss}";
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);


        public static IEnumerable<IEvaluator> Evaluators
        {
            get; set;
        } = [];

        public static IServiceProvider? ServiceProvider { get; private set; }


        [AssemblyInitialize]
        public static async Task BaseInitialize(TestContext context)
        {
            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.AgentEvalsWorkshop_AppHost>();

            var services = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    // override the logging filters
                    logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                    logging.AddFilter("Aspire.", LogLevel.Debug);
                    logging.AddOpenTelemetry();
                    logging.AddConsole();
                });
            
            s_appHost = await appHost.BuildAsync();

            await s_appHost.ResourceNotifications
                .WaitForResourceAsync("chat", KnownResourceStates.Running, context.CancellationTokenSource.Token);
            await s_appHost.ResourceNotifications.WaitForResourceHealthyAsync(
                "agents", context.CancellationTokenSource.Token)
                .WaitAsync(DefaultTimeout, context.CancellationTokenSource.Token);

            var chatConnectionString = await s_appHost
                .GetConnectionStringAsync("chat", context.CancellationTokenSource.Token);

            if (string.IsNullOrEmpty(chatConnectionString))
            {
                throw new InvalidOperationException("Run aspire app host first, and ensure the chat deployment is available.");
            }

            s_chatConfiguration = GetAzureOpenAIChatConfiguration(chatConnectionString)
                ?? throw new InvalidOperationException("Failed to create ChatConfiguration from connection string.");

            services.AddSingleton(s_chatConfiguration.ChatClient);
            ServiceProvider =
                services.BuildServiceProvider();
        }

        private static ChatConfiguration GetAzureOpenAIChatConfiguration(string connectionString)
        {
            var chatConfiguration = new FoundryConnectionStringParts();
            chatConfiguration.ParseConnectionString(connectionString);

            var credential = chatConfiguration.TokenCredential ?? new DefaultAzureCredential();
            var options = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview)
            {
                Audience = "https://cognitiveservices.azure.com/.default",
            };
            var baseUri = new Uri(chatConfiguration.Endpoint.AbsoluteUri.Replace("/models", "").Replace("services.ai", "cognitiveservices"));
            IChatClient azureClient = new AzureOpenAIClient(
                    baseUri,
                    new AzureCliCredential()
                )
                .GetChatClient(chatConfiguration.Deployment)
                .AsIChatClient();

            return new ChatConfiguration(azureClient);
        }


        [AssemblyCleanup]
        public static async Task ClassCleanup()
        {
            if (s_appHost != null)
            {
                await s_appHost.DisposeAsync();
            }
        }
    }
}
