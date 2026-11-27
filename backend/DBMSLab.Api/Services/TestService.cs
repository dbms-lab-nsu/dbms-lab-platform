using DBMSLab.Api.Contracts;
using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Exceptions;
using DBMSLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Services;

/// <summary>Test business logic implementation with access checks and result counting.</summary>
/// <param name="tests">Reads tests and questions.</param>
/// <param name="attempts">Reads and writes attempts.</param>
/// <param name="enrollments">Checks user enrollment in a course.</param>
/// <param name="db">Database context for checking course access rights.</param>
public sealed class TestService(
    ITestRepository tests,
    ITestAttemptRepository attempts,
    IEnrollmentRepository enrollments,
    AppDbContext db) : ITestService
{
    private const int PassingScorePercent = 60;

    /// <inheritdoc />
    public async Task<TestDto> GetTestAsync(
        Guid testId,
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var test = await tests.GetTestForClientAsync(testId, cancellationToken);
        if (test is null)
            throw new KeyNotFoundException("Test not found.");

        var course = await GetTestCourseAsync(test, cancellationToken);
        if (course is null)
            throw new KeyNotFoundException("Test not found.");

        if (!CanAccessTest(course.AuthorId, course.Status, user))
            throw new AccessDeniedException("You do not have access to this test.");

        return MapToTestDto(test);
    }

    /// <inheritdoc />
    public async Task<TestAttemptResultDto> SubmitAttemptAsync(
        Guid testId,
        System.Security.Claims.ClaimsPrincipal user,
        Guid userId,
        SubmitTestAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        var test = await tests.GetTestWithAnswersAsync(testId, cancellationToken);
        if (test is null)
            throw new KeyNotFoundException("Test not found.");

        var course = await GetTestCourseAsync(test, cancellationToken);
        if (course is null)
            throw new KeyNotFoundException("Test not found.");

        if (!CanAccessTest(course.AuthorId, course.Status, user))
            throw new AccessDeniedException("You do not have access to this test.");

        var isEnrolled = await enrollments.ExistsAsync(userId, course.Id, cancellationToken);
        if (!isEnrolled)
            throw new AccessDeniedException("You are not enrolled in the course this test belongs to.");

        var questions = test.Questions.OrderBy(q => q.SortOrder).ToList();
        var totalQuestions = questions.Count;

        if (totalQuestions == 0)
            throw new InvalidOperationException("The test has no questions.");

        var answersByQuestion = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Check: each test question must have exactly one answer
        foreach (var question in questions)
        {
            if (!answersByQuestion.TryGetValue(question.Id, out var questionAnswers) || questionAnswers.Count != 1)
                throw new InvalidOperationException(
                    $"Question {question.Id} must have exactly one answer.");
        }

        // Check: each selected option must belong to the corresponding question
        var correctOptionIds = questions
            .ToDictionary(q => q.Id, q => q.AnswerOptions
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .FirstOrDefault());

        var optionToQuestion = questions
            .SelectMany(q => q.AnswerOptions.Select(o => new { OptionId = o.Id, QuestionId = q.Id }))
            .ToDictionary(x => x.OptionId, x => x.QuestionId);

        var userAnswers = new List<TestAnswer>();
        int correctAnswers = 0;

        foreach (var question in questions)
        {
            var answerRequest = answersByQuestion[question.Id].First();
            var answerOptionId = answerRequest.AnswerOptionId;

            if (!optionToQuestion.TryGetValue(answerOptionId, out var owningQuestionId) || owningQuestionId != question.Id)
                throw new InvalidOperationException(
                    $"Answer option {answerOptionId} does not belong to question {question.Id}.");

            var isCorrect = correctOptionIds.TryGetValue(question.Id, out var correctId) && correctId == answerOptionId;
            if (isCorrect)
                correctAnswers++;

            userAnswers.Add(new TestAnswer
            {
                QuestionId = question.Id,
                AnswerOptionId = answerOptionId,
                IsCorrect = isCorrect
            });
        }

        int scorePercent = totalQuestions == 0 ? 0 : (int)Math.Round((double)correctAnswers / totalQuestions * 100);
        bool isPassed = scorePercent >= PassingScorePercent;

        var attempt = new TestAttempt
        {
            TestId = testId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ScorePercent = scorePercent,
            IsPassed = isPassed,
            Answers = userAnswers
        };

        await attempts.AddAttemptAsync(attempt, cancellationToken);

        return new TestAttemptResultDto(
            attempt.Id,
            attempt.TestId,
            attempt.UserId,
            attempt.StartedAt,
            attempt.CompletedAt,
            attempt.ScorePercent,
            attempt.IsPassed,
            totalQuestions,
            correctAnswers,
            userAnswers.Select(a => new TestAttemptAnswerResultDto(a.QuestionId, a.IsCorrect)).ToList()
        );
    }

    /// <inheritdoc />
    public async Task<TestAttemptResultDto> GetAttemptAsync(
        Guid attemptId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await attempts.GetAttemptWithAnswersAsync(attemptId, cancellationToken);
        if (attempt is null)
            throw new KeyNotFoundException("Attempt not found.");

        if (attempt.UserId != userId)
            throw new AccessDeniedException("You can view only your own attempts.");

        return new TestAttemptResultDto(
            attempt.Id,
            attempt.TestId,
            attempt.UserId,
            attempt.StartedAt,
            attempt.CompletedAt,
            attempt.ScorePercent,
            attempt.IsPassed,
            attempt.Answers.Count,
            attempt.Answers.Count(a => a.IsCorrect),
            attempt.Answers.Select(a => new TestAttemptAnswerResultDto(a.QuestionId, a.IsCorrect)).ToList()
        );
    }

    /// <summary>Checks whether the user has access to the test via the course.</summary>
    /// <param name="courseAuthorId">Course author read from the database.</param>
    /// <param name="courseStatus">Course status read from the database.</param>
    /// <param name="user">Current user (may be null for service checks).</param>
    /// <returns>Whether access is allowed.</returns>
    private bool CanAccessTest(Guid courseAuthorId, CourseStatus courseStatus, System.Security.Claims.ClaimsPrincipal? user)
    {
        if (courseStatus == CourseStatus.Published)
            return user?.Identity?.IsAuthenticated == true;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (user.IsInRole(AppRoles.Administrator))
            return true;

        return user.IsInRole(AppRoles.Author)
            && System.Guid.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            && userId == courseAuthorId;
    }

    /// <summary>Loads the course the test belongs to via topic and module.</summary>
    private Task<Course?> GetTestCourseAsync(Test test, CancellationToken cancellationToken) =>
        db.Topics
            .Include(t => t.Module).ThenInclude(m => m.Course)
            .Where(t => t.Id == test.TopicId)
            .Select(t => t.Module.Course)
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>Maps a test domain entity to a client DTO (without IsCorrect on options).</summary>
    private static TestDto MapToTestDto(Test test) =>
        new(
            test.Id,
            test.Title,
            test.Description,
            test.SortOrder,
            test.Questions.OrderBy(q => q.SortOrder)
                .Select(q => new TestQuestionDto(
                    q.Id,
                    q.Text,
                    q.SortOrder,
                    q.AnswerOptions.OrderBy(o => o.SortOrder)
                        .Select(o => new AnswerOptionDto(o.Id, o.Text, o.SortOrder))
                        .ToList()))
                .ToList());
}
