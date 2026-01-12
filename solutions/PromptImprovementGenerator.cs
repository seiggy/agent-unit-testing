// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.AI;
using System.Text;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Generates improved agent instructions based on evaluation results.
/// This is not a traditional evaluator but uses the evaluation infrastructure 
/// to analyze failures and suggest prompt improvements.
/// </summary>
public sealed class PromptImprovementGenerator
{
    private readonly IChatClient _chatClient;

    public PromptImprovementGenerator(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Generates an improved prompt based on the current instructions and evaluation failures.
    /// </summary>
    /// <param name="currentInstructions">The current agent instructions.</param>
    /// <param name="evaluationResults">Results from running evaluation tests.</param>
    /// <param name="gameRules">The rules the agent should follow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A suggested improved prompt.</returns>
    public async Task<PromptImprovementResult> GenerateImprovedPromptAsync(
        string currentInstructions,
        IEnumerable<TestEvaluationResult> evaluationResults,
        string gameRules,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildImprovementPrompt(currentInstructions, evaluationResults, gameRules);

        var response = await _chatClient.GetResponseAsync<PromptImprovementResponse>(
            prompt,
            cancellationToken: cancellationToken);

        if (!response.TryGetResult(out var result))
        {
            return new PromptImprovementResult(
                Success: false,
                ImprovedPrompt: currentInstructions,
                Explanation: "Failed to generate improved prompt.",
                TargetedFixes: []
            );
        }

        return new PromptImprovementResult(
            Success: true,
            ImprovedPrompt: result.ImprovedPrompt,
            Explanation: result.Explanation,
            TargetedFixes: result.TargetedFixes
        );
    }

    private static List<ChatMessage> BuildImprovementPrompt(
        string currentInstructions,
        IEnumerable<TestEvaluationResult> evaluationResults,
        string gameRules)
    {
        var resultsBuilder = new StringBuilder();
        foreach (var testResult in evaluationResults)
        {
            resultsBuilder.AppendLine($"## Test: {testResult.TestName}");
            resultsBuilder.AppendLine($"Status: {(testResult.Passed ? "✅ PASSED" : "❌ FAILED")}");
            resultsBuilder.AppendLine($"User Input: {testResult.UserInput}");
            resultsBuilder.AppendLine($"Agent Response: {testResult.AgentResponse}");
            
            if (testResult.Violations.Any())
            {
                resultsBuilder.AppendLine("Violations:");
                foreach (var violation in testResult.Violations)
                {
                    resultsBuilder.AppendLine($"  - {violation}");
                }
            }
            
            if (!string.IsNullOrEmpty(testResult.FailureReason))
            {
                resultsBuilder.AppendLine($"Failure Reason: {testResult.FailureReason}");
            }
            
            resultsBuilder.AppendLine();
        }

        var prompt = $$"""
            You are an expert prompt engineer. Your task is to improve an AI agent's system prompt 
            based on evaluation test results that show where the agent is failing to follow the rules.

            ## Current Agent Instructions
            ```
            {{currentInstructions}}
            ```

            ## Rules the Agent Must Follow
            {{gameRules}}

            ## Evaluation Test Results
            {{resultsBuilder}}

            ## Your Task
            Analyze the failures and create an improved system prompt that will help the agent 
            pass all the tests. The improved prompt should:

            1. Be clear and specific about WHEN to use each tool
            2. Include explicit step-by-step instructions for each scenario
            3. Address each failure by adding the missing instruction
            4. Keep the prompt concise but complete
            5. Use formatting (headers, numbered lists) for clarity

            Respond in this JSON format:
            {
                "improvedPrompt": "The complete new system prompt...",
                "explanation": "Brief explanation of what was changed and why",
                "targetedFixes": [
                    { "issue": "Agent didn't ask first question", "fix": "Added step 3 to explicitly get and ask first question" },
                    ...
                ]
            }

            IMPORTANT: The improvedPrompt should be a complete, ready-to-use system prompt, not a diff or partial update.
            """;

        return [new ChatMessage(ChatRole.User, prompt)];
    }
}

/// <summary>
/// Result of a single test evaluation for prompt improvement analysis.
/// </summary>
public record TestEvaluationResult(
    string TestName,
    bool Passed,
    string UserInput,
    string AgentResponse,
    IReadOnlyList<string> Violations,
    string? FailureReason
);

/// <summary>
/// Result of prompt improvement generation.
/// </summary>
public record PromptImprovementResult(
    bool Success,
    string ImprovedPrompt,
    string Explanation,
    IReadOnlyList<TargetedFix> TargetedFixes
);

/// <summary>
/// Response structure for prompt improvement.
/// </summary>
public record PromptImprovementResponse(
    string ImprovedPrompt,
    string Explanation,
    TargetedFix[] TargetedFixes
);

/// <summary>
/// A specific fix applied to the prompt.
/// </summary>
public record TargetedFix(
    string Issue,
    string Fix
);
