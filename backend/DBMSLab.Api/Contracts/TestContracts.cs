using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DBMSLab.Api.Contracts;

/// <summary>Response for getting a test. Correct answers are not exposed to the client.</summary>
public sealed record TestDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<TestQuestionDto> Questions)
{
    /// <summary>Fixed element type: test.</summary>
    public string Type => "test";
}

/// <summary>Test question without correct-answer flags on options.</summary>
public sealed record TestQuestionDto(
    Guid Id,
    string Text,
    int SortOrder,
    IReadOnlyList<AnswerOptionDto> AnswerOptions)
{
    /// <summary>Fixed element type: test question.</summary>
    public string Type => "test_question";
}

/// <summary>Answer option without correctness flag (hidden until attempt submission).</summary>
public sealed record AnswerOptionDto(
    Guid Id,
    string Text,
    int SortOrder)
{
    /// <summary>Fixed element type: answer option.</summary>
    public string Type => "answer_option";
}

/// <summary>Request to submit answers for a test attempt.</summary>
public sealed record SubmitTestAttemptRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<TestAttemptAnswerRequest> Answers { get; init; } = Array.Empty<TestAttemptAnswerRequest>();
}

/// <summary>User's answer to one question in an attempt.</summary>
public sealed record TestAttemptAnswerRequest
{
    [Required]
    public Guid QuestionId { get; init; }

    [Required]
    public Guid AnswerOptionId { get; init; }
}

/// <summary>Result of a test attempt.</summary>
public sealed record TestAttemptResultDto(
    Guid Id,
    Guid TestId,
    Guid UserId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int ScorePercent,
    bool IsPassed,
    int TotalQuestions,
    int CorrectAnswers,
    IReadOnlyList<TestAttemptAnswerResultDto> Answers)
{
    /// <summary>Fixed element type: test attempt.</summary>
    public string Type => "test_attempt";
}

/// <summary>Result of one question answer with correctness flag.</summary>
public sealed record TestAttemptAnswerResultDto(
    Guid QuestionId,
    bool IsCorrect)
{
    /// <summary>Fixed element type: test attempt answer.</summary>
    public string Type => "test_attempt_answer";
}
