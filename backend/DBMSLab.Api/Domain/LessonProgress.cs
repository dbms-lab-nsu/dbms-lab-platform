namespace DBMSLab.Api.Domain;

public enum LessonProgressStatus
{
    NotStarted,
    InProgress,
    Completed
}

public sealed class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public LessonProgressStatus Status { get; set; } = LessonProgressStatus.NotStarted;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
