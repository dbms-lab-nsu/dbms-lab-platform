using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

/// <summary>Builds content API responses while enforcing course and lesson publication rules.</summary>
/// <param name="courses">Reads stored content and its publication state.</param>
/// <remarks>Authentication is enforced by the API; this service checks publication only.</remarks>
public sealed class CourseService(ICourseRepository courses)
{
    /// <summary>Returns published course cards with pagination metadata.</summary>
    /// <param name="query">Validated page settings: a page of at least 1 and a page size from 1 to 100.</param>
    /// <param name="cancellationToken">Cancels the repository queries.</param>
    /// <returns>A catalog page with the total count before pagination; items are empty beyond the last page.</returns>
    /// <remarks>The API validates <paramref name="query"/>; direct callers must validate it themselves.</remarks>
    public async Task<CourseCatalogDto> GetCatalogAsync(
        CourseCatalogQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await courses.CountCoursesAsync(CourseStatus.Published, cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
            return new CourseCatalogDto([], query.Page, query.PageSize, totalCount);

        var page = await courses.GetCoursesAsync(
            CourseStatus.Published, (int)offset, query.PageSize, cancellationToken);
        var items = page.Select(course => new CourseDto(
            course.Id, course.Title, course.Description)).ToList();
        return new CourseCatalogDto(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Returns a published course card without loading its hierarchy.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="cancellationToken">Cancels the repository query.</param>
    /// <returns>The course title, description, and public access metadata.</returns>
    /// <exception cref="ContentNotFoundException">The course is missing, a draft, or archived.</exception>
    public async Task<CourseDto> GetCourseAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await courses.GetCourseAsync(id, cancellationToken);
        if (course is null || course.Status != CourseStatus.Published)
            throw new ContentNotFoundException("The course was not found.");

        return new CourseDto(course.Id, course.Title, course.Description);
    }

    /// <summary>Builds the public navigation tree for a published course.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="cancellationToken">Cancels the repository queries.</param>
    /// <returns>Ordered modules and topics with published lesson summaries; empty containers are preserved.</returns>
    /// <exception cref="ContentNotFoundException">The course is missing, a draft, or archived.</exception>
    /// <remarks>Unpublished lessons are removed. Lesson content is never included.</remarks>
    public async Task<CourseStructureDto> GetStructureAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await courses.GetStructureAsync(id, cancellationToken);
        if (course is null || course.Status != CourseStatus.Published)
            throw new ContentNotFoundException("The course was not found.");

        return new CourseStructureDto(course.Id, course.Title, course.Description,
            course.Modules.Select(module => new ModuleDto(
                module.Id, module.Title, module.Description, module.SortOrder,
                module.Topics.Select(topic => new TopicDto(
                    topic.Id, topic.Title, topic.Description, topic.SortOrder,
                    topic.Lessons.Where(lesson => lesson.IsPublished)
                        .Select(lesson => new LessonSummaryDto(lesson.Id, lesson.Title, lesson.SortOrder))
                        .ToList())).ToList())).ToList());
    }

    /// <summary>Returns a lesson only when both it and its course are published.</summary>
    /// <param name="id">The lesson identifier.</param>
    /// <param name="cancellationToken">Cancels the repository query.</param>
    /// <returns>The stored content and the identifiers locating the lesson in its course.</returns>
    /// <exception cref="ContentNotFoundException">
    /// The lesson is missing or unpublished, or its course is a draft or archived.
    /// </exception>
    public async Task<LessonDto> GetLessonAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await courses.GetLessonAsync(id, cancellationToken);
        if (lesson is null || !lesson.IsPublished || lesson.CourseStatus != CourseStatus.Published)
            throw new ContentNotFoundException("The lesson was not found.");

        return new LessonDto(lesson.Id, lesson.CourseId, lesson.ModuleId, lesson.TopicId,
            lesson.Title, lesson.SortOrder, lesson.Content);
    }
}
