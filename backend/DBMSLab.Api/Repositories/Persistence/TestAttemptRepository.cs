using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories.Persistence;

/// <summary>Works with test attempts and user answers in PostgreSQL.</summary>
/// <param name="db">Database context.</param>
public sealed class TestAttemptRepository(AppDbContext db) : ITestAttemptRepository
{
    /// <inheritdoc />
    public Task<TestAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken) =>
        db.TestAttempts
            .AsNoTracking()
            .Where(a => a.Id == attemptId)
            .Include(a => a.Test)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question)
            .Include(a => a.Answers).ThenInclude(ans => ans.AnswerOption)
            .SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAttemptAsync(TestAttempt attempt, CancellationToken cancellationToken)
    {
        await db.TestAttempts.AddAsync(attempt, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
