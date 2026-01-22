using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentEvalsWorkshop.Agents;


public class KnowledgebaseChatAgent
{
    
    public static AIAgent BuildKnowledgebaseChatAgent(IChatClient chatClient)
    {
        return chatClient
            .AsAIAgent(
            instructions: """
                You are a knowledgebase agent with knowledge about the Xbox Gamepass library.
                Use the knowledgebase to answer user questions about game completion rate, playtime, rating, and achievement score details.
                If the answer is not in the knowledgebase, respond with "I don't know".
                """,
                name: "Assistant",
                tools: GetToolDefinitions()
            );
    }

    public static AITool[] GetToolDefinitions()
    {
        return [AIFunctionFactory.Create(GetKnowledgebase)];
    }


    [Description("Load the knowledge base.")]
    private static async Task<GamePassGameInfo[]?> GetKnowledgebase()
    {
        var csvData = await File.ReadAllLinesAsync("./Data/Gamepass_Games_v1.csv");

        var gameInfoList = new List<GamePassGameInfo>();
        foreach (var line in csvData.Skip(1)) // Skip header
        {
            var parts = line.Split(',');

            if (parts.Length != 9)
                continue; // Skip malformed lines

            var gameInfo = new GamePassGameInfo(
                Title: parts[0],
                Ratio: TryParseDecimal(parts[1]),
                NumberOfGamers: TryParseInt(parts[2]),
                NumberOfCompletions: TryParseDecimal(parts[3]),
                AverageTimeToCompletion: parts[4],
                ReviewScore: TryParseDecimal(parts[5]),
                DateAddedToGamePass: parts[6],
                TrueAchievementScore: TryParseInt(parts[7]),
                Gamerscore: TryParseInt(parts[8])
            );

            gameInfoList.Add(gameInfo);
        }
        return gameInfoList.ToArray();
    }

    private static int TryParseInt(string intString)
    {
        if (int.TryParse(intString, out var value))
        {
            return value;
        }
        return -1;
    }

    private static decimal TryParseDecimal(string decimalString)
    {
        if (decimal.TryParse(decimalString, out var value))
        {
            return value;
        }
        return -1m;
    }
}

[Description("Information about a game in the Xbox Gamepass library. A value of -1 in any field indicates that the data is unavailable.")]
record GamePassGameInfo(
    [Description("The title of the game.")]
    string Title,
    [Description("The ratio of the TrueAchievementScore to Gamerscore.")]
    decimal Ratio,
    [Description("The number of gamers who have played the game.")]
    int NumberOfGamers,
    [Description("The number of players who have completed the game.")]
    decimal NumberOfCompletions,
    [Description("The average time it took a player to get all achievements.")]
    string AverageTimeToCompletion,
    [Description("The rating out of 5 stars that players have given the game.")]
    decimal ReviewScore,
    [Description("The date the game was added to Xbox Gamepass.")]
    string DateAddedToGamePass,
    [Description("The TrueAchievement score for the game (in game points). The TrueAchievement score is a metric that reflects the difficulty of earning the achievements in the game.")]
    int TrueAchievementScore,
    [Description("The total Gamerscore available in the game.")]
    int Gamerscore
);
