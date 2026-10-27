using DBMSLab.Api.Contracts;
using DBMSLab.Api.Exceptions;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DBMSLab.Api.Controllers;

/// <summary>
/// API for lesson and course progress of the current user.
/// Routes: GET /api/me/courses/{courseId}/progress, PUT /api/me/lessons/{lessonId}/progress.
/// </summary>
[Authorize]
[ApiController]
[Route("api/me")]
public sealed class ProgressController(IProgressService service) : ControllerBase
{
    /// <summary>
    /// Get course progress for the user.
    /// Available only if the user is enrolled in the course.
    /// Percentage is calculated only from published lessons.
    /// </summary>
    /// <param name="courseId">Course identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Course progress.</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="403">User is not enrolled in this course.</response>
    /// <response code="404">Course not found.</response>
    [HttpGet("courses/{courseId}/progress")]
    [ProducesResponseType(typeof(CourseProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseProgress(Guid courseId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse(
                "unauthorized",
                "Could not determine the current user.",
                StatusCodes.Status401Unauthorized));

        try
        {
            var progress = await service.GetCourseProgressAsync(userId.Value, courseId, ct);
            return Ok(progress);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse(
                "not_found",
                "Course not found.",
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

    /// <summary>
    /// Update lesson progress status for the current user.
    /// Available only if the user is enrolled in the course the lesson belongs to.
    /// Repeated lesson completion does not create duplicate records.
    /// </summary>
    /// <param name="lessonId">Lesson identifier.</param>
    /// <param name="request">New lesson progress status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Invalid request or disallowed status transition.</response>
    /// <response code="401">User is not authorized.</response>
    /// <response code="403">User is not enrolled in the lesson's course.</response>
    /// <response code="404">Lesson not found.</response>
    [HttpPut("lessons/{lessonId}/progress")]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLessonProgress(
        Guid lessonId,
        [FromBody] LessonProgressRequest request,
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
                "Invalid lesson progress status.",
                StatusCodes.Status400BadRequest));

        try
        {
            var entity = await service.UpdateLessonProgressAsync(userId.Value, lessonId, request.Status, ct);
            return Ok(new LessonProgressResponse(entity.LessonId, entity.Status, entity.UpdatedAt));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse(
                "not_found",
                "Lesson not found.",
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

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var id) ? null : id;
    }
}
