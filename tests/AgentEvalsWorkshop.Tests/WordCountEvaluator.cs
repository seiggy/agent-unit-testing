using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace AgentEvalsWorkshop.Tests;

public sealed class WordCountEvaluator : IEvaluator
{
    public const string WordCountMetricName = "Words";

    public IReadOnlyCollection<string> EvaluationMetricNames => [ WordCountMetricName ];

    private static int CountWords(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0;
        }

        var matches = Regex.Matches(input!, @"\b\w+\b");
        return matches.Count;
    }

    private static void Interpret(NumericMetric metric)
    {
        if (metric.Value is null)
        {
            metric.Interpretation =
                new EvaluationMetricInterpretation(
                    EvaluationRating.Unknown, 
                    failed: true, 
                    reason: "Failed to calculate the word count for the response.");
        }
        else
        {
            if (metric.Value <= 100 && metric.Value > 5)
            {
                metric.Interpretation =
                    new EvaluationMetricInterpretation(
                        EvaluationRating.Good, 
                        reason: "The response contains between 6 and 100 words.");
            }
            else
            {
                metric.Interpretation =
                    new EvaluationMetricInterpretation(
                        EvaluationRating.Unacceptable, 
                        failed: true,
                        reason: "The response contains 5 or fewer words, or more than 100 words.");
            }
        }
    }

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var wordCount = CountWords(modelResponse.Text);

        var reason = 
            $"This {WordCountMetricName} metric has a value of {wordCount} because the model response contains {wordCount} words.";

        var metric = new NumericMetric(WordCountMetricName,
            value: wordCount,
            reason: reason);

        Interpret(metric);

        return ValueTask.FromResult(new EvaluationResult(metric));
    }
}