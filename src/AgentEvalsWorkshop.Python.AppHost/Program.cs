using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var resourceGroup      = builder.AddParameter("resource-group");
var azFoundryName      = builder.AddParameter("az-foundry-name");
var gptDeployment      = builder.AddParameter("gpt-deployment-name");
var projectEndpoint    = builder.AddParameter("foundry-project-endpoint");

// Resolve the lab-provisioned Foundry account — do NOT call AddDeployment("chat");
// Skillable provisions the `chat` deployment outside of Aspire.
var foundry = builder.AddAzureAIFoundry("az-foundry")
    .AsExisting(azFoundryName, resourceGroup);

builder.AddUvicornApp(
        name: "py-agent",
        appDirectory: "../../src-python",
        app: "agent_evals_workshop.app:app")
    .WithUv()
    .WithReference(foundry)
    .WithEnvironment("CHAT_DEPLOYMENT_NAME", "chat")
    .WithEnvironment("AZURE_OPENAI_DEPLOYMENT_NAME", gptDeployment)
    .WithEnvironment("AZURE_AI_PROJECT_ENDPOINT", projectEndpoint)
    .WithHttpEndpoint(port: 8111, env: "PORT")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
