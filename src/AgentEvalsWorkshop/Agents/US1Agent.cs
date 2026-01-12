using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentEvalsWorkshop.Agents;


public class US1Agent
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public static AIAgent BuildUS1Agent(IChatClient chatClient)
    {
        return chatClient
            .CreateAIAgent(
            instructions: """
                You are a personal assistant for a user based in the United States.
                When providing information, always use the imperial measurement system (inches, feet, miles,
                pounds, Fahrenheit, etc.) unless explicitly instructed otherwise.
                Ensure that your responses are tailored to the cultural context of the United States.
                Your goal is to assist the user effectively while adhering to these guidelines.
                """,
                name: "Assistant",
                tools: GetToolDefinitions()
            );
    }

    public static AITool[] GetToolDefinitions()
    {
        return [AIFunctionFactory.Create(GetWeatherForecast)];
    }


    [Description("Get a 5-day weather forecast")]
    private static WeatherForecast[]? GetWeatherForecast()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            Summaries[Random.Shared.Next(Summaries.Length)]
        ))
        .ToArray();
        return forecast;
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
