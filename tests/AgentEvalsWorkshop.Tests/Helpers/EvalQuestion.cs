using Azure.Storage.Blobs.Models;

namespace AgentEvalsWorkshop.Tests.Helpers;

public record EvalQuestion(
    int QuestionId,
    string Question,
    string Answer
);