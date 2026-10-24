using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBMSLab.Api.Controllers;

[ApiController]
[Route("api/courses")]
[AllowAnonymous]
[TypeFilter(typeof(ContentExceptionFilter))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class CoursesController(CourseService courses) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CourseCatalogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseCatalogDto>> GetCatalog(
        [FromQuery] CourseCatalogQuery query, CancellationToken cancellationToken) =>
        Ok(await courses.GetCatalogAsync(query, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetCourseAsync(id, cancellationToken));

    [HttpGet("{id}/structure")]
    [ProducesResponseType(typeof(CourseStructureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseStructureDto>> GetStructure(Guid id, CancellationToken cancellationToken) =>
        Ok(await courses.GetStructureAsync(id, cancellationToken));
}
