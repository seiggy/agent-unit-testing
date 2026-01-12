using AgentEvalsWorkshop.Agents;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;
using Azure.AI.OpenAI;
using Aspire.Azure.AI.Inference;
using Microsoft.Extensions.Azure;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var endpoint = new Uri($"{builder.Configuration["CHAT_URI"]}/openai/v1/");

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
        .GetChatClient("chat")
        .AsIChatClient();
});

//builder.AddAzureOpenAIClient(connectionName: "foundry", (settings) =>
//    {
//        settings.EnableSensitiveTelemetryData = true;
//    }, (clientBuilder) =>
//    {
//        clientBuilder.ConfigureOptions(options =>
//        {

//        });
//    })
//    .AddChatClient("chat");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapA2A(US1Agent.BuildUS1Agent(app.Services.GetRequiredService<IChatClient>()), "/us1agent");

app.Run();


