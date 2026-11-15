using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Repositories;

/// <summary>Access to test attempt data and user answers.</summary>
public interface ITestAttemptRepository
{
    /// <summary>Returns an attempt with answers by identifier.</summary>
    /// <param name="attemptId">Attempt identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Attempt with answers, or null.</returns>
    Task<TestAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken);

    /// <summary>Adds a new attempt and saves changes.</summary>
    /// <param name="attempt">Attempt to add.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    Task AddAttemptAsync(TestAttempt attempt, CancellationToken cancellationToken);
}
