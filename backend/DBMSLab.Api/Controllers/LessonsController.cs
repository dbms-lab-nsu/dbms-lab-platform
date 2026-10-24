using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

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
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonDto>> GetLesson(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetLessonAsync(id, cancellationToken));
}
