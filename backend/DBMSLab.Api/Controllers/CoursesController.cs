using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

/// <summary>Serves public course reads and lets authors manage their own courses.</summary>
/// <param name="courses">Applies publication rules and builds response DTOs.</param>
[ApiController]
[Route("api/courses")]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(CourseStructureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseStructureDto>> GetStructure(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetStructureAsync(id, User, cancellationToken));

    /// <summary>Creates a draft owned by the authenticated Author.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Author)]
    [ProducesResponseType<AuthorCourseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthorCourseDto>> CreateCourse(
        CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await courses.CreateCourseAsync(request, User, cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    /// <summary>Changes the supplied title or description of an owned draft or published course.</summary>
    /// <response code="409">The course is archived and cannot be edited.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = AppRoles.Author)]
    [ProducesResponseType<AuthorCourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorCourseDto>> UpdateCourse(
        Guid id, UpdateCourseRequest request, CancellationToken cancellationToken) =>
        Ok(await courses.UpdateCourseAsync(id, request, User, cancellationToken));

    /// <summary>Publishes an owned draft and its complete lessons.</summary>
    /// <response code="409">The course is not a draft or is missing required metadata or complete lessons.</response>
    [HttpPost("{id}/publish")]
    [Authorize(Roles = AppRoles.Author)]
    [ProducesResponseType<AuthorCourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorCourseDto>> PublishCourse(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.PublishCourseAsync(id, User, cancellationToken));

    /// <summary>Archives an owned draft or published course without changing its lessons.</summary>
    /// <response code="409">The course is already archived.</response>
    [HttpPost("{id}/archive")]
    [Authorize(Roles = AppRoles.Author)]
    [ProducesResponseType<AuthorCourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthorCourseDto>> ArchiveCourse(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.ArchiveCourseAsync(id, User, cancellationToken));
}
