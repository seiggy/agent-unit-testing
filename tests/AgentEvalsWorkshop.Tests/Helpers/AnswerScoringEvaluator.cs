// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------


namespace Microsoft.Extensions.AI.Evaluation.Quality;

public sealed class AnswerScoringEvaluator : IEvaluator
{
    public IReadOnlyCollection<string> EvaluationMetricNames => throw new NotImplementedException();

    public ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages, ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null, IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

record ScoringResponse(AnswerScore[] Scores);
record AnswerScore(int Index, int ScoreLabel, string DescriptionOfQuality);