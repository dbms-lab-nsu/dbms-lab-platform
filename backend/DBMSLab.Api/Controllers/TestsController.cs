using DBMSLab.Api.Contracts;
using DBMSLab.Api.Exceptions;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DBMSLab.Api.Controllers;

/// <summary>
/// API for getting tests with questions and answer options.
/// Route: GET /api/tests/{id}.
/// Correct answers are not included in the client response.
/// </summary>
[Authorize]
[ApiController]
[Route("api/tests")]
public sealed class TestsController(ITestService tests) : ControllerBase
{
    /// <summary>
    /// Returns a test with questions and answer options.
    /// Correct answer options are hidden from the client.
    /// Available to authenticated users with access to the course.
    /// </summary>
    /// <param name="id">Test identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Test with questions (without correct answers).</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="403">No access to the test.</response>
    /// <response code="404">Test not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTest(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        try
        {
            var testDto = await tests.GetTestAsync(id, User, ct);
            return Ok(testDto);
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
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var id) ? null : id;
    }
}
