using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Repositories;

/// <summary>Access to test data and their questions with answer options.</summary>
public interface ITestRepository
{
    /// <summary>Returns a test with questions and answer options (without IsCorrect flag on options) for the client.</summary>
    /// <param name="testId">Test identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Test with nested questions and options, or null.</returns>
    Task<Test?> GetTestForClientAsync(Guid testId, CancellationToken cancellationToken);

    /// <summary>Returns a test with questions and options (including IsCorrect flag) for server-side checking.</summary>
    /// <param name="testId">Test identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Test with all data, or null.</returns>
    Task<Test?> GetTestWithAnswersAsync(Guid testId, CancellationToken cancellationToken);
}
