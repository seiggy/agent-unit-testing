using System.Data.Common;
using Azure.Core;
using Azure.Identity;

namespace AgentEvalsWorkshop.Tests;

public sealed class FoundryConnectionStringParts
{
    public Uri Endpoint { get; private set; } = null!;
    public string? ApiKey { get; private set; }
    public string? Deployment { get; private set; } = null!;
    public TokenCredential? TokenCredential { get; private set; }
    public Uri OpenAIEndpoint { get; private set;  } = null!;

    internal void ParseConnectionString(string? connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder()
        {
            ConnectionString = connectionString
        };

        var deploymentKeys = new List<string>();

        if (connectionBuilder.ContainsKey("Deployment"))
        {
            deploymentKeys.Add("Deployment");
        }
        if (connectionBuilder.ContainsKey("DeploymentId"))
        {
            deploymentKeys.Add("DeploymentId");
        }
        if (connectionBuilder.ContainsKey("Model"))
        {
            deploymentKeys.Add("Model");
        }

        if (deploymentKeys.Count > 1)
        {
            throw new ArgumentException("Connection string cannot contain more than one of 'Deployment', 'DeploymentId', or 'Model' keys.");
        }

        if (connectionBuilder.TryGetValue("Deployment", out var deployment))
        {
            Deployment = deployment as string;
        }

        if (connectionBuilder.TryGetValue("DeploymentId", out var deploymentId))
        {
            Deployment = deploymentId as string;
        }

        if (connectionBuilder.TryGetValue("Model", out var model))
        {
            Deployment = model as string;
        }

        // Use EnpointAIInferrence if available, fallback to Endpoint
        if (connectionBuilder.TryGetValue("EndpointAIInference", out var endpoint) && Uri.TryCreate(endpoint as string, UriKind.Absolute, out var serviceUri))
        {
            Endpoint = serviceUri;

            // swap the AI Inference path to OpenAI path
            if (Endpoint.Segments.Contains("models"))
            {
                var openAiEndpoint = endpoint as string;
                openAiEndpoint = openAiEndpoint!.Replace("/models", "/openai/v1");
                OpenAIEndpoint = new Uri(openAiEndpoint);
            }
            else
            {
                OpenAIEndpoint = Endpoint;
            }
        }
        else if (connectionBuilder.TryGetValue("Endpoint", out endpoint) && Uri.TryCreate(endpoint as string, UriKind.Absolute, out serviceUri))
        {
            Endpoint = serviceUri;

            // swap the AI Inference path to OpenAI path
            if (Endpoint.Segments.Contains("models"))
            {
                var openAiEndpoint = endpoint as string;
                openAiEndpoint = openAiEndpoint!.Replace("/models", "/openai/v1");
                OpenAIEndpoint = new Uri(openAiEndpoint);
            }
            else
            {
                OpenAIEndpoint = Endpoint;
            }
        }
        
        if (connectionBuilder.TryGetValue("Key", out var key) && key is string apiKey)
        {
            ApiKey = apiKey;
        }
        else
        {
            TokenCredential = new DefaultAzureCredential();
        }
    }
}
