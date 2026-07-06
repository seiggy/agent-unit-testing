using AgentEvalsWorkshop.Agents;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;
using Azure.AI.OpenAI;
using Aspire.Azure.AI.Inference;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using AgentEvalsWorkshop.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var endpoint = ConnectionStringParser.GetEndpointFromConnectionString(builder.Configuration.GetConnectionString("az-foundry")!);
var foundryDeploymentName = builder.Configuration["FOUNDRY_DEPLOYMENT_NAME"]!;

var azureOpenAIClient = new AzureOpenAIClient(
    endpoint,
    new DefaultAzureCredential(),
    new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview)
    {
        ClientLoggingOptions = new System.ClientModel.Primitives.ClientLoggingOptions
        {
            EnableMessageContentLogging = true,
            EnableLogging = true,
        }
    });

builder.Services.AddSingleton(_ => new AzureOpenAIClient(
    endpoint,
    new DefaultAzureCredential(),
    new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview)
    {
        ClientLoggingOptions = new System.ClientModel.Primitives.ClientLoggingOptions
        {
            EnableMessageContentLogging = true,
            EnableLogging = true,
        }
    }));

builder.Services.AddSingleton<IChatClient>(sp =>
{
    return sp.GetRequiredService<AzureOpenAIClient>()
        .GetChatClient(foundryDeploymentName)
        .AsIChatClient();
});

builder.Services.AddA2AServer(US1Agent.BuildUS1Agent(azureOpenAIClient.GetChatClient(foundryDeploymentName).AsIChatClient()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapA2AJsonRpc(US1Agent.BuildUS1Agent(app.Services.GetRequiredService<IChatClient>()), "/us1agent");

app.Run();


