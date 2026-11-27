using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories.Persistence;

/// <summary>Reads tests and questions from PostgreSQL using EF Core without change tracking.</summary>
/// <param name="db">Database context.</param>
public sealed class TestRepository(AppDbContext db) : ITestRepository
{
    /// <inheritdoc />
    public Task<Test?> GetTestForClientAsync(Guid testId, CancellationToken cancellationToken) =>
        db.Tests
            .AsNoTracking()
            .Where(t => t.Id == testId)
            .Include(t => t.Topic).ThenInclude(topic => topic.Module).ThenInclude(module => module.Course)
            .Include(t => t.Questions).ThenInclude(q => q.AnswerOptions)
            .Select(t => new Test
            {
                Id = t.Id,
                TopicId = t.TopicId,
                Title = t.Title,
                Description = t.Description,
                SortOrder = t.SortOrder,
                CreatedAtUtc = t.CreatedAtUtc,
                Questions = t.Questions.OrderBy(q => q.SortOrder)
                    .Select(q => new TestQuestion
                    {
                        Id = q.Id,
                        TestId = q.TestId,
                        Text = q.Text,
                        SortOrder = q.SortOrder,
                        AnswerOptions = q.AnswerOptions.OrderBy(o => o.SortOrder)
                            .Select(o => new AnswerOption
                            {
                                Id = o.Id,
                                QuestionId = o.QuestionId,
                                Text = o.Text,
                                SortOrder = o.SortOrder
                                // IsCorrect is not populated: correct answers are hidden from the client
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Test?> GetTestWithAnswersAsync(Guid testId, CancellationToken cancellationToken) =>
        db.Tests
            .AsNoTracking()
            .Where(t => t.Id == testId)
            .Include(t => t.Topic).ThenInclude(topic => topic.Module).ThenInclude(module => module.Course)
            .Include(t => t.Questions).ThenInclude(q => q.AnswerOptions)
            .SingleOrDefaultAsync(cancellationToken);
}
