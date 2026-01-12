using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Azure.Core;
using Azure.Provisioning.CognitiveServices;
using Humanizer.Localisation;
using System.Linq;

var builder = DistributedApplication.CreateBuilder(args);

// Azure PostgreSQL instance
var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
var postgresDb = postgres.AddDatabase("agentevalsworkshopdb");

// Azure Foundry
var foundry = builder.AddAzureAIFoundry("foundry");
var gpt4o = foundry.AddDeployment("chat", AIFoundryModel.OpenAI.Gpt4o)
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

var eastUs = builder.AddAzureAIFoundry("eastus2-foundry")
    .ConfigureInfrastructure(infra =>
    {
        var resources = infra.GetProvisionableResources();
        var account = resources.OfType<CognitiveServicesAccount>().Single();
        account.Location = AzureLocation.EastUS2;
    });

var gpt52chat = eastUs.AddDeployment("smarter-chat", AIFoundryModel.OpenAI.Gpt52Chat)
    .WithProperties(deployment =>
    {
        deployment.SkuCapacity = 50;
    });


// Agent project
var agent = builder.AddProject<Projects.AgentEvalsWorkshop>("agents")
    .WithReference(foundry)
    .WithReference(eastUs)
    .WithReference(postgresDb)
    .WithRoleAssignments(foundry, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor)
    .WithReference(gpt4o)
    .WithReference(gpt52chat);

builder.Build().Run();
