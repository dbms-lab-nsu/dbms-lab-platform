using DBMSLab.Api.Contracts;
using DBMSLab.Api.Filters;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Reads and updates progress owned by the authenticated account.</summary>
[Authorize]
[ApiController]
[Route("api/me")]
[TypeFilter(typeof(ContentExceptionFilter))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public sealed class ProgressController(IProgressService service, LearningAccessService access) : ControllerBase
{
    /// <summary>Returns progress for an enrolled course, including archived courses.</summary>
    /// <remarks>Counts completed published lessons; returns zero percent if no lessons are published.</remarks>
    [HttpGet("courses/{courseId}/progress")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<CourseProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<CourseProgressDto>> GetCourseProgress(Guid courseId, CancellationToken ct)
    {
        var userId = await access.RequireUserIdAsync(User);
        return Ok(await service.GetCourseProgressAsync(userId, courseId, ct));
    }

    /// <summary>Sets progress for a published lesson of a published course the caller is enrolled in.</summary>
    /// <remarks>Accepts named states, including resets. Repeating a state preserves updatedAt.</remarks>
    /// <response code="400">The identifier, JSON, or required status is invalid.</response>
    /// <response code="403">The lesson or course is unpublished, or the caller is not enrolled.</response>
    /// <response code="404">The lesson does not exist.</response>
    [HttpPut("lessons/{lessonId}/progress")]
    [ProducesResponseType<LessonProgressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<LessonProgressResponse>> UpdateLessonProgress(
        Guid lessonId, LessonProgressRequest request, CancellationToken ct)
    {
        var userId = await access.RequireUserIdAsync(User);
        var progress = await service.UpdateLessonProgressAsync(userId, lessonId, request.Status!.Value, ct);
        return Ok(new LessonProgressResponse(progress.LessonId, progress.Status, progress.UpdatedAt));
    }
}
