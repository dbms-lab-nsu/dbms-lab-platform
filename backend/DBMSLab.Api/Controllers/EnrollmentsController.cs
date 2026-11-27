using DBMSLab.Api.Contracts;
using DBMSLab.Api.Filters;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Creates and lists enrollments for the authenticated account.</summary>
[Authorize]
[ApiController]
[Route("api/me/enrollments")]
[TypeFilter(typeof(ContentExceptionFilter))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public sealed class EnrollmentsController(IEnrollmentService service, LearningAccessService access) : ControllerBase
{
    /// <summary>Enrolls in a published course; retries return the original enrollment.</summary>
    /// <response code="400">The course identifier is not a GUID.</response>
    /// <response code="403">The course is a draft or archive, including when an enrollment already exists.</response>
    /// <response code="404">The course does not exist.</response>
    [HttpPut("{courseId}")]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(Guid courseId, CancellationToken ct)
    {
        var userId = await access.RequireUserIdAsync(User);
        var enrollment = await service.EnrollAsync(userId, courseId, ct);
        return Ok(new EnrollmentResponse(enrollment.CourseId, enrollment.EnrolledAt));
    }

    /// <summary>Lists the current account's enrollments, retaining archived course history.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EnrollmentListResponse>>> GetMy(CancellationToken ct)
    {
        var userId = await access.RequireUserIdAsync(User);
        var enrollments = await service.GetUserEnrollmentsAsync(userId, ct);
        return Ok(enrollments.Select(e => new EnrollmentListResponse(
            e.CourseId, e.Course?.Title ?? string.Empty, e.EnrolledAt)));
    }
}
