using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Serves published lesson content to callers authenticated by the existing JWT scheme.</summary>
/// <param name="courses">Checks publication state and builds the lesson response.</param>
[ApiController]
[Route("api/lessons")]
[Authorize]
[TypeFilter(typeof(ContentExceptionFilter))]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class LessonsController(CourseService courses) : ControllerBase
{
    /// <summary>Gets stored lesson content when both the lesson and its course are published.</summary>
    /// <param name="id">The lesson identifier from the route.</param>
    /// <param name="cancellationToken">Cancels database work if the request is aborted.</param>
    /// <returns>HTTP 200 with content and parent identifiers.</returns>
    /// <response code="400">An authenticated request contains an invalid GUID.</response>
    /// <response code="401">The caller has no valid JWT; authentication runs before lesson lookup.</response>
    /// <response code="404">The lesson is missing or unpublished, or its course is not published.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonDto>> GetLesson(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetLessonAsync(id, cancellationToken));
}
