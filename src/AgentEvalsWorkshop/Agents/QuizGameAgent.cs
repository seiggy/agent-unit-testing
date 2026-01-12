using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace AgentEvalsWorkshop.Agents;

/// <summary>
/// A quiz game agent that can create and play trivia games.
/// The agent has tools to manage questions but minimal instructions on how to use them.
/// </summary>
public class QuizGameAgent
{
    // Thread-safe storage for quiz questions, keyed by session ID
    private static readonly ConcurrentDictionary<string, List<TriviaQuestion>> s_questionSets = new();
    
    // Track current question index per session
    private static readonly ConcurrentDictionary<string, int> s_currentQuestionIndex = new();
    
    // Track user scores per session
    private static readonly ConcurrentDictionary<string, List<QuestionResult>> s_sessionResults = new();

    public static AIAgent BuildQuizGameAgent(IChatClient chatClient, string instructions)
    {
        return chatClient
            .CreateAIAgent(
            instructions: instructions,
                name: "QuizMaster",
                tools: GetToolDefinitions()
            );
    }

    public static AITool[] GetToolDefinitions()
    {
        return [
            AIFunctionFactory.Create(CreateQuizQuestions),
            AIFunctionFactory.Create(GetCurrentQuestion),
            AIFunctionFactory.Create(ScoreQuestion),
            AIFunctionFactory.Create(MoveToNextQuestion),
            AIFunctionFactory.Create(GetQuizProgress),
            AIFunctionFactory.Create(GenerateScoreboard)
        ];
    }

    /// <summary>
    /// Creates a set of trivia questions for a quiz session.
    /// </summary>
    [Description("Creates a set of trivia questions for a quiz game session. Returns the session ID to use for the game.")]
    private static string CreateQuizQuestions(
        [Description("Array of trivia questions to create for the quiz. Should contain up to 10 questions.")] 
        TriviaQuestion[] questions,
        [Description("Optional session ID. If not provided, a new session will be created.")]
        string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString("N")[..8];
        
        // Ensure we have at most 10 questions
        var questionList = questions.Take(10).ToList();
        
        // Assign IDs if not provided
        for (int i = 0; i < questionList.Count; i++)
        {
            if (questionList[i].Id == 0)
            {
                questionList[i] = questionList[i] with { Id = i + 1 };
            }
        }
        
        s_questionSets[sessionId] = questionList;
        s_currentQuestionIndex[sessionId] = 0;
        s_sessionResults[sessionId] = [];
        
        return sessionId;
    }

    /// <summary>
    /// Gets the current question for a quiz session.
    /// </summary>
    [Description("Gets the current trivia question for the specified session. Returns the question without the answer.")]
    private static QuestionDisplay? GetCurrentQuestion(
        [Description("The session ID for the quiz game.")]
        string sessionId)
    {
        if (!s_questionSets.TryGetValue(sessionId, out var questions))
        {
            return null;
        }
        
        if (!s_currentQuestionIndex.TryGetValue(sessionId, out var index) || index >= questions.Count)
        {
            return null;
        }
        
        var question = questions[index];
        return new QuestionDisplay(
            question.Id,
            question.Category,
            question.Question,
            index + 1,
            questions.Count
        );
    }

    /// <summary>
    /// Scores the user's response to the current question. The AI determines correctness.
    /// </summary>
    [Description("Records the score for the current question. The AI should determine if the user's answer is correct by comparing it to the stored answer, handling variations like typos, alternate phrasings, or partial matches. Call this after evaluating the user's response.")]
    private static ScoreResult ScoreQuestion(
        [Description("The session ID for the quiz game.")]
        string sessionId,
        [Description("Whether the user's answer was correct. The AI should determine this by comparing the user's response to the correct answer, being lenient with typos, alternate phrasings, and semantically equivalent answers.")]
        bool wasCorrect,
        [Description("The user's answer as they provided it (for record keeping).")]
        string userAnswer)
    {
        if (!s_questionSets.TryGetValue(sessionId, out var questions))
        {
            return new ScoreResult(false, "Session not found", "", false);
        }
        
        if (!s_currentQuestionIndex.TryGetValue(sessionId, out var index) || index >= questions.Count)
        {
            return new ScoreResult(false, "No current question", "", false);
        }
        
        var question = questions[index];
        var correctAnswer = question.Answer;
        
        // Store the result
        if (s_sessionResults.TryGetValue(sessionId, out var results))
        {
            results.Add(new QuestionResult(
                question.Id,
                question.Question,
                question.Category,
                correctAnswer,
                userAnswer,
                wasCorrect
            ));
        }
        
        var isLastQuestion = index >= questions.Count - 1;
        
        return new ScoreResult(true, wasCorrect ? "Correct!" : "Incorrect", correctAnswer, isLastQuestion);
    }

    /// <summary>
    /// Moves to the next question in the quiz.
    /// </summary>
    [Description("Moves to the next question in the quiz session. Returns true if there are more questions, false if the quiz is complete.")]
    private static bool MoveToNextQuestion(
        [Description("The session ID for the quiz game.")]
        string sessionId)
    {
        if (!s_currentQuestionIndex.TryGetValue(sessionId, out var index))
        {
            return false;
        }
        
        if (!s_questionSets.TryGetValue(sessionId, out var questions))
        {
            return false;
        }
        
        if (index >= questions.Count - 1)
        {
            return false; // No more questions
        }
        
        s_currentQuestionIndex[sessionId] = index + 1;
        return true;
    }

    /// <summary>
    /// Gets the current progress of the quiz.
    /// </summary>
    [Description("Gets the current progress of the quiz including questions answered and current score.")]
    private static QuizProgress GetQuizProgress(
        [Description("The session ID for the quiz game.")]
        string sessionId)
    {
        if (!s_questionSets.TryGetValue(sessionId, out var questions))
        {
            return new QuizProgress(0, 0, 0, false);
        }
        
        if (!s_currentQuestionIndex.TryGetValue(sessionId, out var index))
        {
            return new QuizProgress(0, questions.Count, 0, false);
        }
        
        var correctCount = 0;
        if (s_sessionResults.TryGetValue(sessionId, out var results))
        {
            correctCount = results.Count(r => r.WasCorrect);
        }
        
        var isComplete = index >= questions.Count - 1 && results?.Count >= questions.Count;
        
        return new QuizProgress(index + 1, questions.Count, correctCount, isComplete);
    }

    /// <summary>
    /// Generates an HTML scoreboard for the completed quiz.
    /// </summary>
    [Description("Generates a fun HTML scoreboard showing the user's performance on each question. Only call this when all questions have been answered.")]
    private static string GenerateScoreboard(
        [Description("The session ID for the quiz game.")]
        string sessionId)
    {
        if (!s_sessionResults.TryGetValue(sessionId, out var results) || results.Count == 0)
        {
            return "<html><body><h1>No results found</h1></body></html>";
        }
        
        var correctCount = results.Count(r => r.WasCorrect);
        var totalQuestions = results.Count;
        var percentage = (double)correctCount / totalQuestions * 100;
        
        var emoji = percentage switch
        {
            >= 90 => "🏆",
            >= 70 => "🌟",
            >= 50 => "👍",
            _ => "📚"
        };
        
        var message = percentage switch
        {
            100 => "Perfect Score! You're a trivia genius!",
            >= 90 => "Outstanding! You really know your stuff!",
            >= 70 => "Great job! You're quite knowledgeable!",
            >= 50 => "Not bad! Keep learning!",
            _ => "Keep practicing! You'll get better!"
        };
        
        var questionsHtml = string.Join("\n", results.Select((r, i) => $@"
            <div class='question-row {(r.WasCorrect ? "correct" : "incorrect")}'>
                <div class='question-number'>Q{i + 1}</div>
                <div class='question-details'>
                    <div class='category'>{r.Category}</div>
                    <div class='question-text'>{r.Question}</div>
                    <div class='answers'>
                        <span class='your-answer'>Your answer: {r.UserAnswer}</span>
                        <span class='correct-answer'>Correct: {r.CorrectAnswer}</span>
                    </div>
                </div>
                <div class='result-icon'>{(r.WasCorrect ? "✅" : "❌")}</div>
            </div>"));
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Quiz Results</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; margin: 0; padding: 20px; }}
        .container {{ max-width: 800px; margin: 0 auto; background: white; border-radius: 20px; padding: 30px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .emoji {{ font-size: 80px; }}
        .score {{ font-size: 48px; font-weight: bold; color: #333; }}
        .message {{ font-size: 24px; color: #666; margin-top: 10px; }}
        .progress-bar {{ background: #e0e0e0; border-radius: 10px; height: 20px; margin: 20px 0; overflow: hidden; }}
        .progress-fill {{ background: linear-gradient(90deg, #4CAF50, #8BC34A); height: 100%; transition: width 0.5s; }}
        .question-row {{ display: flex; align-items: center; padding: 15px; margin: 10px 0; border-radius: 10px; }}
        .question-row.correct {{ background: #e8f5e9; }}
        .question-row.incorrect {{ background: #ffebee; }}
        .question-number {{ font-size: 24px; font-weight: bold; color: #666; width: 50px; }}
        .question-details {{ flex: 1; }}
        .category {{ font-size: 12px; color: #999; text-transform: uppercase; letter-spacing: 1px; }}
        .question-text {{ font-size: 16px; color: #333; margin: 5px 0; }}
        .answers {{ font-size: 14px; }}
        .your-answer {{ color: #666; margin-right: 20px; }}
        .correct-answer {{ color: #4CAF50; font-weight: bold; }}
        .result-icon {{ font-size: 24px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>{emoji}</div>
            <div class='score'>{correctCount}/{totalQuestions}</div>
            <div class='message'>{message}</div>
            <div class='progress-bar'>
                <div class='progress-fill' style='width: {percentage}%'></div>
            </div>
        </div>
        <h2>Question Breakdown</h2>
        {questionsHtml}
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Clears all quiz data (useful for testing).
    /// </summary>
    public static void ClearAllSessions()
    {
        s_questionSets.Clear();
        s_currentQuestionIndex.Clear();
        s_sessionResults.Clear();
    }
}

/// <summary>
/// Represents a trivia question with its answer.
/// </summary>
[Description("A trivia question for the quiz game.")]
public record TriviaQuestion(
    [Description("Unique identifier for the question.")]
    int Id,
    [Description("The category of the question (e.g., 'Science', 'History', 'Sports').")]
    string Category,
    [Description("The trivia question text.")]
    string Question,
    [Description("The correct answer to the question.")]
    string Answer
);

/// <summary>
/// A question display without the answer (shown to users).
/// </summary>
[Description("A question displayed to the user (without the answer).")]
public record QuestionDisplay(
    int Id,
    string Category,
    string Question,
    int QuestionNumber,
    int TotalQuestions
);

/// <summary>
/// Result of scoring a question.
/// </summary>
[Description("The result of scoring a user's answer.")]
public record ScoreResult(
    bool Success,
    string Message,
    string CorrectAnswer,
    bool IsLastQuestion
);

/// <summary>
/// Current progress in the quiz.
/// </summary>
[Description("Current progress in the quiz game.")]
public record QuizProgress(
    int CurrentQuestion,
    int TotalQuestions,
    int CorrectAnswers,
    bool IsComplete
);

/// <summary>
/// Result for a single question.
/// </summary>
public record QuestionResult(
    int QuestionId,
    string Question,
    string Category,
    string CorrectAnswer,
    string UserAnswer,
    bool WasCorrect
);