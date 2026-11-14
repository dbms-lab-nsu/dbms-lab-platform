using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Serves the private course list of the authenticated author.</summary>
[ApiController]
[Authorize(Roles = AppRoles.Author)]
[Route("api/author/courses")]
[TypeFilter(typeof(ContentExceptionFilter))]
public sealed class AuthorCoursesController(CourseService courses) : ControllerBase
{
    /// <summary>Lists the caller's drafts, published courses, and archives in title and identifier order.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(IReadOnlyList<AuthorCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<AuthorCourseDto>>> GetCourses(CancellationToken cancellationToken) =>
        Ok(await courses.GetAuthorCoursesAsync(User, cancellationToken));
}
