using DBMSLab.Api.Contracts;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;

namespace DBMSLab.Api.Controllers;

/// <summary>
/// API for user enrollment in courses.
/// Routes: PUT /api/me/enrollments/{courseId}, GET /api/me/enrollments.
/// </summary>
[Authorize]
[ApiController]
[Route("api/me/enrollments")]
public sealed class EnrollmentsController(IEnrollmentService service) : ControllerBase
{
    /// <summary>
    /// Start taking a published course.
    /// Repeated requests do not create duplicate records.
    /// </summary>
    /// <param name="courseId">Course identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Record created or already existed.</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="404">Course not found.</response>
    /// <response code="400">Course is not published and cannot be started.</response>
    /// <response code="409">Unique record conflict (concurrent enrollment in the same course).</response>
    [HttpPut("{courseId}")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(Guid courseId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        try
        {
            var enrollment = await service.EnrollAsync(userId.Value, courseId, ct);
            return Ok(new EnrollmentResponse(enrollment.CourseId, enrollment.EnrolledAt));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse(
                "not_found",
                "Course not found.",
                StatusCodes.Status404NotFound));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(
                "bad_request",
                ex.Message,
                StatusCodes.Status400BadRequest));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Race condition: another transaction created the record first.
            var existing = (await service.GetUserEnrollmentsAsync(userId.Value, ct))
                .FirstOrDefault(e => e.CourseId == courseId);
            if (existing is not null)
                return Ok(new EnrollmentResponse(existing.CourseId, existing.EnrolledAt));

            return Conflict(new ErrorResponse(
                "conflict",
                "Enrollment in this course already exists.",
                StatusCodes.Status409Conflict));
        }
    }

    /// <summary>
    /// Get the list of courses the current user is enrolled in.
    /// Results are sorted by course title.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of user's enrollments.</response>
    /// <response code="401">User is not authorized.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        var enrollments = await service.GetUserEnrollmentsAsync(userId.Value, ct);
        var result = enrollments.Select(e => new EnrollmentListResponse(
            e.CourseId,
            e.Course?.Title ?? string.Empty,
            e.EnrolledAt));
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var id) ? null : id;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == "23505";
    }
}
