using System.Security.Claims;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

/// <summary>Builds content API responses using publication rules and caller-specific preview permissions.</summary>
/// <param name="courses">Reads stored content and its publication state.</param>
/// <remarks>The API validates JWTs; this service checks course ownership and roles for previews.</remarks>
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

    /// <summary>Returns a published course card or an authorized draft or archive preview.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="user">The caller populated by the API authentication middleware.</param>
    /// <param name="cancellationToken">Cancels the repository query.</param>
    /// <returns>The course title, description, and authentication requirement.</returns>
    /// <exception cref="ContentNotFoundException">The course does not exist.</exception>
    /// <exception cref="ContentAccessDeniedException">The course exists but the caller cannot preview it.</exception>
    public async Task<CourseDto> GetCourseAsync(Guid id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var course = await courses.GetCourseAsync(id, cancellationToken);
        if (course is null)
            throw new ContentNotFoundException("The course was not found.");

        if (course.Status != CourseStatus.Published && !CanPreviewCourse(course.AuthorId, user))
            throw new ContentAccessDeniedException("You do not have access to this course.");

        return new CourseDto(course.Id, course.Title, course.Description)
        {
            RequiresAuthentication = course.Status != CourseStatus.Published
        };
    }

    /// <summary>Builds a course navigation tree using the caller's preview permissions.</summary>
    /// <param name="id">The course identifier.</param>
    /// <param name="user">The caller populated by the API authentication middleware.</param>
    /// <param name="cancellationToken">Cancels the repository queries.</param>
    /// <returns>Ordered modules and topics with visible lesson summaries; empty containers are preserved.</returns>
    /// <exception cref="ContentNotFoundException">The course does not exist.</exception>
    /// <exception cref="ContentAccessDeniedException">The course exists but the caller cannot preview it.</exception>
    /// <remarks>Lesson summaries include caller-specific access flags; lesson content is never included.</remarks>
    public async Task<CourseStructureDto> GetStructureAsync(Guid id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var course = await courses.GetStructureAsync(id, cancellationToken);
        if (course is null)
            throw new ContentNotFoundException("The course was not found.");

        var canPreview = CanPreviewCourse(course.AuthorId, user);
        if (course.Status != CourseStatus.Published && !canPreview)
            throw new ContentAccessDeniedException("You do not have access to this course.");

        var requiresAuthentication = course.Status != CourseStatus.Published;
        return new CourseStructureDto(course.Id, course.Title, course.Description,
            course.Modules.Select(module => new ModuleDto(
                module.Id, module.Title, module.Description, module.SortOrder,
                module.Topics.Select(topic => new TopicDto(
                    topic.Id, topic.Title, topic.Description, topic.SortOrder,
                    topic.Lessons.Where(lesson => lesson.IsPublished || canPreview)
                        .Select(lesson => new LessonSummaryDto(lesson.Id, lesson.Title, lesson.SortOrder)
                        {
                            IsAccessible = CanAccessLesson(lesson.IsPublished, course.Status, course.AuthorId, user)
                        })
                        .ToList())
                {
                    RequiresAuthentication = requiresAuthentication
                }).ToList())
            {
                RequiresAuthentication = requiresAuthentication
            }).ToList())
        {
            RequiresAuthentication = requiresAuthentication
        };
    }

    /// <summary>Returns published lesson content or an authorized preview of unpublished content.</summary>
    /// <param name="id">The lesson identifier.</param>
    /// <param name="user">The authenticated caller populated by the API authentication middleware.</param>
    /// <param name="cancellationToken">Cancels the repository query.</param>
    /// <returns>The stored content and the identifiers locating the lesson in its course.</returns>
    /// <exception cref="ContentNotFoundException">The lesson does not exist.</exception>
    /// <exception cref="ContentAccessDeniedException">The lesson exists but the caller cannot read its content.</exception>
    public async Task<LessonDto> GetLessonAsync(Guid id, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var lesson = await courses.GetLessonAsync(id, cancellationToken);
        if (lesson is null)
            throw new ContentNotFoundException("The lesson was not found.");

        if (!CanAccessLesson(lesson.IsPublished, lesson.CourseStatus, lesson.CourseAuthorId, user))
            throw new ContentAccessDeniedException("You do not have access to this lesson.");

        return new LessonDto(lesson.Id, lesson.CourseId, lesson.ModuleId, lesson.TopicId,
            lesson.Title, lesson.SortOrder, lesson.Content);
    }

    /// <summary>Applies the same content access rule to lesson summaries and lesson content requests.</summary>
    /// <param name="isPublished">Whether the lesson is published.</param>
    /// <param name="courseStatus">The publication state of the parent course.</param>
    /// <param name="courseAuthorId">The parent course owner read from the database.</param>
    /// <param name="user">The caller's validated identity and role claims.</param>
    /// <returns>Whether the authenticated caller may read published content or preview this lesson.</returns>
    private static bool CanAccessLesson(
        bool isPublished, CourseStatus courseStatus, Guid courseAuthorId, ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && ((isPublished && courseStatus == CourseStatus.Published) || CanPreviewCourse(courseAuthorId, user));

    /// <summary>Allows administrators or an authenticated Author who owns the requested course to preview it.</summary>
    /// <param name="authorId">The course owner read from the database.</param>
    /// <param name="user">The caller's validated identity and role claims.</param>
    /// <returns>Whether the caller may bypass publication filtering for this course.</returns>
    private static bool CanPreviewCourse(Guid authorId, ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        if (user.IsInRole(AppRoles.Administrator))
            return true;

        return user.IsInRole(AppRoles.Author)
            && Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            && userId == authorId;
    }
}
