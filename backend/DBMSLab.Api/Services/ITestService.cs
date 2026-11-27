using DBMSLab.Api.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Services;

/// <summary>Test business logic: getting a test, checking answers, saving results.</summary>
public interface ITestService
{
    /// <summary>Returns a test with questions for the client (without correct answers).</summary>
    /// <param name="testId">Test identifier.</param>
    /// <param name="user">Authenticated user.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="KeyNotFoundException">Test not found.</exception>
    /// <exception cref="AccessDeniedException">No access to the test.</exception>
    Task<TestDto> GetTestAsync(Guid testId, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Submits answers to a test, checks them, and saves the attempt result.</summary>
    /// <param name="testId">Test identifier.</param>
    /// <param name="user">Authenticated user.</param>
    /// <param name="userId">Identifier of the current user.</param>
    /// <param name="request">User answers.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="KeyNotFoundException">Test not found.</exception>
    /// <exception cref="AccessDeniedException">No access to the test or attempt.</exception>
    /// <exception cref="InvalidOperationException">Invalid answers (option does not belong to question, etc.).</exception>
    Task<TestAttemptResultDto> SubmitAttemptAsync(
        Guid testId,
        System.Security.Claims.ClaimsPrincipal user,
        Guid userId,
        SubmitTestAttemptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns an attempt result only to its owner.</summary>
    /// <param name="attemptId">Attempt identifier.</param>
    /// <param name="userId">Identifier of the current user.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="KeyNotFoundException">Attempt not found.</exception>
    /// <exception cref="AccessDeniedException">Attempt belongs to another user.</exception>
    Task<TestAttemptResultDto> GetAttemptAsync(
        Guid attemptId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
