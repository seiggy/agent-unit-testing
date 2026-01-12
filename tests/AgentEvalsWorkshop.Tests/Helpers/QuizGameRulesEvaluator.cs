// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Evaluates how well a Quiz Game Agent follows the defined game rules.
/// </summary>
public sealed class QuizGameRulesEvaluator : IEvaluator
{
    /// <summary>
    /// Context containing the game rules to evaluate against.
    /// </summary>
    public sealed class Context : EvaluationContext
    {
        private const string ContextName = "Quiz Game Rules";

        public Context(string currentInstructions) 
            : base(ContextName, content: GameRules)
        {
            CurrentInstructions = currentInstructions;
        }

        public string CurrentInstructions { get; }

        /// <summary>
        /// The rules that the Quiz Game Agent must follow.
        /// </summary>
        public static string GameRules => """
            The Quiz Game Agent must follow these rules:

            RULE 1 - Start Game: When a user asks to start a game, generate exactly 10 trivia questions and answers using the CreateQuizQuestions tool.

            RULE 2 - Category Support: If the user asks for a specific category (e.g., Science, History, Sports), generate ALL questions from that category.

            RULE 3 - Ask First Question: After creating questions, immediately retrieve and ask the user the first question using GetCurrentQuestion. Do not wait for another prompt.

            RULE 4 - Score Answers: When the user provides an answer, compare it to the correct answer (be lenient with typos/variations), then call ScoreQuestion to record if they were correct or incorrect. Tell the user if they were right or wrong.

            RULE 5 - Progress Through Questions: After scoring an answer, use MoveToNextQuestion and GetCurrentQuestion to present the next question. Continue until all questions are answered.

            RULE 6 - Generate Scoreboard: When all questions have been answered, use GenerateScoreboard to create a fun HTML scoreboard showing the user's performance on each question.
            """;
    }

    private const string MetricName = "Quiz Game Rules Adherence";
    private const string RuleViolationsMetricName = "Rule Violations";

    public const string QuizGameRulesMetricName = MetricName;
    public const string QuizGameRuleViolationsMetricName = RuleViolationsMetricName;

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName, RuleViolationsMetricName];

    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelResponse);
        ArgumentNullException.ThrowIfNull(chatConfiguration);

        var adherenceMetric = new NumericMetric(MetricName);
        var violationsMetric = new StringMetric(RuleViolationsMetricName);
        var result = new EvaluationResult(adherenceMetric, violationsMetric);

        if (!messages.TryGetUserRequest(out ChatMessage? userRequest, out IReadOnlyList<ChatMessage> conversationHistory))
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("Could not extract user request from messages."));
            return result;
        }

        var context = additionalContext?.OfType<Context>().FirstOrDefault();
        var currentInstructions = context?.CurrentInstructions ?? "No instructions provided";

        var evaluationPrompt = BuildEvaluationPrompt(
            userRequest,
            modelResponse,
            conversationHistory,
            currentInstructions);

        var response = await chatConfiguration.ChatClient.GetResponseAsync<RulesEvaluationResponse>(
            evaluationPrompt,
            cancellationToken: cancellationToken);

        if (!response.TryGetResult(out var evalResponse))
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("Failed to parse evaluation response."));
            return result;
        }

        // Calculate overall score (average of rule scores)
        var overallScore = evalResponse.RuleEvaluations.Average(r => r.Score);
        adherenceMetric.Value = overallScore;

        // Collect violations
        var violations = evalResponse.RuleEvaluations
            .Where(r => r.Score < 4)
            .Select(r => $"RULE {r.RuleNumber}: {r.RuleName} - {r.Reason} (Score: {r.Score}/5)")
            .ToList();

        violationsMetric.Value = violations.Count > 0 
            ? string.Join("\n", violations) 
            : "No violations detected.";

        // Add detailed diagnostics
        foreach (var ruleEval in evalResponse.RuleEvaluations)
        {
            var diagnostic = ruleEval.Score >= 4
                ? EvaluationDiagnostic.Informational($"✅ Rule {ruleEval.RuleNumber} ({ruleEval.RuleName}): {ruleEval.Reason}")
                : EvaluationDiagnostic.Warning($"❌ Rule {ruleEval.RuleNumber} ({ruleEval.RuleName}): {ruleEval.Reason}");
            adherenceMetric.AddDiagnostics(diagnostic);
        }

        if (!string.IsNullOrEmpty(evalResponse.SuggestedImprovement))
        {
            adherenceMetric.AddDiagnostics(
                EvaluationDiagnostic.Informational($"💡 Suggestion: {evalResponse.SuggestedImprovement}"));
        }

        adherenceMetric.Interpretation = Interpret(adherenceMetric);
        violationsMetric.Interpretation = new EvaluationMetricInterpretation(
            violations.Count == 0 ? EvaluationRating.Exceptional : EvaluationRating.Poor,
            failed: violations.Count > 0);

        return result;
    }

    private static List<ChatMessage> BuildEvaluationPrompt(
        ChatMessage userRequest,
        ChatResponse modelResponse,
        IReadOnlyList<ChatMessage> conversationHistory,
        string currentInstructions)
    {
        var renderedResponse = modelResponse.RenderText();
        var renderedRequest = userRequest.RenderText();
        
        // Render conversation history
        var historyText = string.Join("\n", conversationHistory.Select(m => 
            $"[{m.Role}]: {m.Text}"));

        var prompt = $$"""
            You are evaluating a Quiz Game Agent's response against specific game rules.

            ## Current Agent Instructions
            ```
            {{currentInstructions}}
            ```

            ## Game Rules to Evaluate
            {{Context.GameRules}}

            ## Conversation History
            {{historyText}}

            ## User Request
            {{renderedRequest}}

            ## Agent Response
            {{renderedResponse}}

            ## Your Task
            Evaluate how well the agent's response follows each rule. For each rule, provide:
            1. A score from 1-5 (1=completely violated, 5=perfectly followed)
            2. A brief reason explaining the score
            3. Only evaluate rules that are applicable to this interaction

            Also provide a specific suggestion for how the agent's instructions could be improved.

            Respond in this JSON format:
            {
                "ruleEvaluations": [
                    { "ruleNumber": 1, "ruleName": "Start Game", "score": 5, "reason": "..." },
                    { "ruleNumber": 2, "ruleName": "Category Support", "score": 5, "reason": "..." },
                    ...
                ],
                "suggestedImprovement": "Add explicit instruction to..."
            }

            Only include rules that are relevant to this specific interaction. For example, if the user is answering a question, you don't need to evaluate the "Start Game" rule.
            """;

        return [new ChatMessage(ChatRole.User, prompt)];
    }

    private static EvaluationMetricInterpretation Interpret(NumericMetric metric)
    {
        var score = metric?.Value ?? 0;
        var rating = score switch
        {
            >= 4.5 => EvaluationRating.Exceptional,
            >= 3.5 => EvaluationRating.Good,
            >= 2.5 => EvaluationRating.Average,
            >= 1.5 => EvaluationRating.Poor,
            _ => EvaluationRating.Unacceptable
        };
        return new EvaluationMetricInterpretation(rating, failed: score < 3.5);
    }
}

/// <summary>
/// Response structure for rules evaluation.
/// </summary>
public record RulesEvaluationResponse(
    RuleEvaluation[] RuleEvaluations,
    string SuggestedImprovement
);

/// <summary>
/// Evaluation of a single rule.
/// </summary>
public record RuleEvaluation(
    int RuleNumber,
    string RuleName,
    int Score,
    string Reason
);
