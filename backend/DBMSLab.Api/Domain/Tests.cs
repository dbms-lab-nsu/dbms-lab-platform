namespace DBMSLab.Api.Domain;

/// <summary>Test associated with course content (topic).</summary>
public sealed class Test
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TopicId { get; set; }
    public CourseTopic Topic { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}

/// <summary>Test question with exactly one correct answer option.</summary>
public sealed class TestQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    public ICollection<TestAnswer> UserAnswers { get; set; } = new List<TestAnswer>();
}

/// <summary>Answer option for a test question. Exactly one option per question may be marked correct.</summary>
public sealed class AnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public TestQuestion Question { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
    public ICollection<TestAnswer> SelectedBy { get; set; } = new List<TestAnswer>();
}

/// <summary>User's test attempt.</summary>
public sealed class TestAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int ScorePercent { get; set; }
    public bool IsPassed { get; set; }
    public ICollection<TestAnswer> Answers { get; set; } = new List<TestAnswer>();
}

/// <summary>User's answer to one question within a single attempt.</summary>
public sealed class TestAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public TestAttempt Attempt { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public TestQuestion Question { get; set; } = null!;
    public Guid AnswerOptionId { get; set; }
    public AnswerOption AnswerOption { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
