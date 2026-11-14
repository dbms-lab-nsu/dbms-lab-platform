using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Serves the public catalog and course details with author and administrator previews.</summary>
/// <param name="courses">Applies publication rules and builds response DTOs.</param>
[ApiController]
[Route("api/courses")]
[AllowAnonymous]
[TypeFilter(typeof(ContentExceptionFilter))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class CoursesController(CourseService courses) : ControllerBase
{
    /// <summary>Gets a page of published course cards.</summary>
    /// <param name="query">Optional page and page-size parameters bound from the query string.</param>
    /// <param name="cancellationToken">Cancels database work if the request is aborted.</param>
    /// <returns>HTTP 200 with items and pagination metadata.</returns>
    /// <response code="400">Pagination parameters are outside their allowed range or cannot be parsed.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CourseCatalogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseCatalogDto>> GetCatalog(
        [FromQuery] CourseCatalogQuery query, CancellationToken cancellationToken) =>
        Ok(await courses.GetCatalogAsync(query, cancellationToken));

    /// <summary>Gets a visible course card, including drafts and archives for authorized previewers.</summary>
    /// <param name="id">The course identifier from the route.</param>
    /// <param name="cancellationToken">Cancels database work if the request is aborted.</param>
    /// <returns>HTTP 200 with the course card.</returns>
    /// <response code="400">The route identifier is not a valid GUID.</response>
    /// <response code="403">The course exists but the caller lacks preview permission.</response>
    /// <response code="404">The course does not exist.</response>
    [HttpGet("{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetCourseAsync(id, User, cancellationToken));

    /// <summary>Gets a course navigation tree without loading protected lesson content.</summary>
    /// <param name="id">The course identifier from the route.</param>
    /// <param name="cancellationToken">Cancels database work if the request is aborted.</param>
    /// <returns>HTTP 200 with ordered modules, topics, and visible lesson summaries.</returns>
    /// <response code="400">The route identifier is not a valid GUID.</response>
    /// <response code="403">The course exists but the caller lacks preview permission.</response>
    /// <response code="404">The course does not exist.</response>
    [HttpGet("{id}/structure")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(CourseStructureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseStructureDto>> GetStructure(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetStructureAsync(id, User, cancellationToken));
}
