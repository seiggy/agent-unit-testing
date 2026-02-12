#pragma warning disable AIEVAL001
using AgentEvalsWorkshop.Agents;
using AgentEvalsWorkshop.Tests.Helpers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace AgentEvalsWorkshop.Tests;

[TestClass]
public class AgentRetrievalEvalTests : BaseIntegrationTest
{
    private static ReportingConfiguration? s_defaultReportingConfiguration;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        Evaluators = GetEvaluators();
        s_defaultReportingConfiguration = DiskBasedReportingConfiguration.Create(
                storageRootPath: "C:\\TestReports",
                evaluators: Evaluators,
                chatConfiguration: ChatConfiguration,
                enableResponseCaching: true,
                executionName: ExecutionName
            );
    }

    private static IEnumerable<IEvaluator> GetEvaluators()
    {
        var relevanceEvaluator = new RelevanceEvaluator();
        var coherenceEvaluator = new CoherenceEvaluator();
        var groundednessEvaluator = new GroundednessEvaluator();
        var answerScoringEvaluator = new AnswerScoringEvaluator();
        return [ relevanceEvaluator, coherenceEvaluator, groundednessEvaluator, answerScoringEvaluator ];
    }    

    private static async Task EvaluateQuestion(
        EvalQuestion question, 
        ReportingConfiguration reportingConfiguration, 
        AIAgent agent,
        CancellationToken cancellationToken)
    {
        // Create a Scenario Run for each question.
        await using ScenarioRun scenario = await reportingConfiguration.CreateScenarioRunAsync($"Question_{question.QuestionId}", cancellationToken: cancellationToken);

        // create a session to track the Q&A interaction
        var session = await agent.CreateSessionAsync();
        var chatHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, question.Question)
        };

        var response = await agent.RunAsync(
            chatHistory,
            session: session,
            cancellationToken: cancellationToken
        );
        chatHistory.AddRange(response.Messages);


        var result = await scenario.EvaluateAsync(
            messages: [new ChatMessage(ChatRole.User, question.Question)],
            modelResponse: response.ToChatResponse(),
            additionalContext: [new AnswerScoringEvaluator.Context(question.Answer),
                new GroundednessEvaluatorContext(await GetKnowledgebaseContext())],
            cancellationToken: cancellationToken
        );

        Validate(result);
    }

    private static async Task<string> GetKnowledgebaseContext()
    {
        var csvData = await File.ReadAllLinesAsync("./Data/Gamepass_Games_v1.csv");

        return $"""
            The following is the knowledge base about Xbox Gamepass games in CSV format:
            {string.Join("\n", csvData)}
            """;
    }

    private static void Validate(EvaluationResult result)
    {
        // Retrieve the score for relevance from the <see cref="EvaluationResult"/>.
        NumericMetric relevance =
            result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        Assert.IsFalse(relevance.Interpretation?.Failed, relevance.Reason);
        Assert.IsTrue(relevance.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);

        // Retrieve the score for coherence from the <see cref="EvaluationResult"/>.
        NumericMetric coherence =
            result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
        Assert.IsFalse(coherence.Interpretation?.Failed, coherence.Reason);
        Assert.IsTrue(coherence.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
                
        // Retrieve the score for groundedness from the <see cref="EvaluationResult"/>.
        NumericMetric groundedness =
            result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        Assert.IsFalse(groundedness.Interpretation?.Failed, groundedness.Reason);
        Assert.IsTrue(groundedness.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
        
        // Retrieve the score for answer quality from the <see cref="EvaluationResult"/>.
        NumericMetric answerScore = result.Get<NumericMetric>(AnswerScoringEvaluator.AnswerScoreMetricName);
        Assert.IsTrue(answerScore.Interpretation?.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
        Assert.IsFalse(answerScore.Interpretation?.Failed, answerScore.Reason);
    }

    [TestMethod]
    [DataRow(1, "What game can I quickly play to get 1000 gamerscore?", "The shortest known game to achieve 1000 gamerscore on Xbox Gamepass is 'Townscaper', which can be completed in approximately 30 minutes!")]
    [DataRow(2, "Which game on Xbox Gamepass has the highest completion rate?", "The game with the highest completion rate on Xbox Gamepass is 'The Walking Dead: Michonne' with a completion rate of 84.7%!")]
    [DataRow(3, "What do gamers think of 'Forza Horizon 5'?", "Forza Horizon 5 currently has a rating of 4.5 out of 5 stars! Gamers love this game!")]
    [DataRow(4, "How long does it typically take to complete 'Halo Infinite'?", "On average, it takes about 100-120 hours to complete all challenges of 'Halo Infinite'.")]
    [DataRow(5, "I'm looking for a game that will take me a long time to finish, preferably an RPG. Any suggestions?", "'Black Desert' is a great choice for a long RPG experience, with an average completion time of 500-750 hours to complete all gamerscope challenges!")]
    public async Task KnowledgebaseChatAgent_EvaluateQuestionAnswer_Scores(int questionId, string questionText, string expectedAnswer)
    {
        var question = new EvalQuestion(questionId, questionText, expectedAnswer);
        using var scope = ServiceProvider!.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var agent = KnowledgebaseChatAgent.BuildKnowledgebaseChatAgent(chatClient);

        await EvaluateQuestion(
            question,
            s_defaultReportingConfiguration!,
            agent,
            TestContext!.CancellationTokenSource.Token);
    }

}
