using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Core;
using Azure.Provisioning.CognitiveServices;
using System;
using System.Linq;

var builder = DistributedApplication.CreateBuilder(args);

var resourceGroup = builder.AddParameter("resource-group");
var azureFoundry = builder.AddParameter("az-foundry-name");
var gptDeployment = builder.AddParameter("gpt-deployment-name");

// Azure Foundry
var foundry = builder.AddAzureAIFoundry("az-foundry")
    .AsExisting(azureFoundry, resourceGroup);

try
{
    var gptDeploymentName = await gptDeployment.Resource.GetValueAsync(System.Threading.CancellationToken.None);

    if (!string.IsNullOrEmpty(gptDeploymentName))
    {
        var gpt52chat = foundry.AddDeployment(gptDeploymentName, AIFoundryModel.OpenAI.Gpt52)
            .WithProperties(deployment =>
            {
                deployment.SkuCapacity = 50;
            });


        // Agent project
        var agent = builder.AddProject<Projects.AgentEvalsWorkshop>("agents")
            .WithReference(foundry)
            .WithEnvironment("FOUNDRY_DEPLOYMENT_NAME", gptDeploymentName)
            .WithReference(gpt52chat);
    }

}
catch (Exception e)
{
    // Ignore errors in getting deployment name
}

builder.Build().Run();
