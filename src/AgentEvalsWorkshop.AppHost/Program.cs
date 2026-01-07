using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Azure.Provisioning.CognitiveServices;

var builder = DistributedApplication.CreateBuilder(args);

// Azure PostgreSQL instance
var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
var postgresDb = postgres.AddDatabase("agentevalsworkshopdb");

// Azure Foundry
var foundry = builder.AddAzureAIFoundry("foundry");
var model = foundry.AddDeployment("chat", AIFoundryModel.OpenAI.Gpt4o)
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });


// Agent project
var agent = builder.AddProject<Projects.AgentEvalsWorkshop>("agents")
    .WithReference(foundry)
    .WithReference(postgresDb)
    .WithRoleAssignments(foundry, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor)
    .WithReference(model);

builder.Build().Run();
