using DBMSLab.Api.Contracts;
using DBMSLab.Api.Exceptions;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DBMSLab.Api.Controllers;

/// <summary>
/// API for submitting test answers and getting results of own attempts.
/// Routes: POST /api/tests/{testId}/attempts, GET /api/me/test-attempts/{id}.
/// </summary>
[Authorize]
[ApiController]
public sealed class TestAttemptsController(ITestService tests) : ControllerBase
{
    /// <summary>
    /// Submits answers to a test for checking. Result is calculated on the server.
    /// Retaking a test is allowed (a new attempt is created).
    /// </summary>
    /// <param name="testId">Test identifier from the route.</param>
    /// <param name="request">User answers to test questions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Attempt result with correct answer count.</response>
    /// <response code="400">Invalid answers (option does not belong to question, etc.).</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="403">No access to the test or course.</response>
    /// <response code="404">Test not found.</response>
    [HttpPost("api/tests/{testId}/attempts")]
    [ProducesResponseType(typeof(TestAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAttempt(
        Guid testId,
        [FromBody] SubmitTestAttemptRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse(
                "bad_request",
                "Invalid attempt answers.",
                StatusCodes.Status400BadRequest));

        try
        {
            var result = await tests.SubmitAttemptAsync(testId, User, userId.Value, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse(
                "not_found",
                "Test not found.",
                StatusCodes.Status404NotFound));
        }
        catch (AccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(
                "forbidden",
                ex.Message,
                StatusCodes.Status403Forbidden));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(
                "bad_request",
                ex.Message,
                StatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Returns the result of the user's own test attempt.
    /// Available only to the attempt owner.
    /// </summary>
    /// <param name="id">Attempt identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Attempt result.</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="403">Attempt belongs to another user.</response>
    /// <response code="404">Attempt not found.</response>
    [HttpGet("api/me/test-attempts/{id}")]
    [ProducesResponseType(typeof(TestAttemptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAttempt(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        try
        {
            var result = await tests.GetAttemptAsync(id, userId.Value, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse(
                "not_found",
                "Test attempt not found.",
                StatusCodes.Status404NotFound));
        }
        catch (AccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(
                "forbidden",
                ex.Message,
                StatusCodes.Status403Forbidden));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var id) ? null : id;
    }
}
