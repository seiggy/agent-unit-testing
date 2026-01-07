using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mono.Cecil.Cil;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public sealed class ConfigurationSmokeTests
{
    public TestContext TestContext { get; set; } = null!;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext.CancellationTokenSource.Token;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AgentEvalsWorkshop_AppHost>();

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);

            // override the logging filters
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            "agents", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        using var httpClient = app.CreateHttpClient("agents");
        using var response = await httpClient.GetAsync("/scalar", cancellationToken);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
