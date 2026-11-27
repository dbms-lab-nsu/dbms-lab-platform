using System.Security.Claims;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

public sealed partial class CourseService
{
    private const int MaxTitleLength = 200;

    /// <summary>Creates an unpublished course owned by the authenticated author.</summary>
    public async Task<AuthorCourseDto> CreateCourseAsync(
        CreateCourseRequest request, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var authorId = await RequireAuthorAsync(user);
        var (title, description) = ValidateMetadata(true, request.Title, true, request.Description);
        var course = new Course
        {
            AuthorId = authorId,
            Title = title!,
            Description = description!,
            Status = CourseStatus.Draft
        };
        await courses.AddCourseAsync(course, cancellationToken);
        return ToAuthorDto(course.Id, course.AuthorId, course.Title, course.Description, course.Status);
    }

    /// <summary>Lists the authenticated author's courses, including drafts and archives.</summary>
    public async Task<IReadOnlyList<AuthorCourseDto>> GetAuthorCoursesAsync(
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var authorId = await RequireAuthorAsync(user);
        var result = await courses.GetCoursesByAuthorAsync(authorId, cancellationToken);
        return result.Select(course => ToAuthorDto(
            course.Id, course.AuthorId, course.Title, course.Description, course.Status)).ToList();
    }

    /// <summary>Updates the supplied metadata of an owned draft or published course.</summary>
    public Task<AuthorCourseDto> UpdateCourseAsync(
        Guid id, UpdateCourseRequest request, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        ChangeOwnedCourseAsync(id, user, (course, _) =>
        {
            if (course.Status is not (CourseStatus.Draft or CourseStatus.Published))
                throw new CourseStateConflictException("Only draft or published courses can be edited.");

            var (title, description) = ValidateMetadata(
                request.HasTitle, request.Title, request.HasDescription, request.Description);
            if (title is not null)
                course.Title = title;
            if (description is not null)
                course.Description = description;
            return Task.CompletedTask;
        }, cancellationToken);

    /// <summary>Publishes an owned draft and exposes its complete lessons in the same transaction.</summary>
    public Task<AuthorCourseDto> PublishCourseAsync(Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        ChangeOwnedCourseAsync(id, user, async (course, ct) =>
        {
            if (course.Status != CourseStatus.Draft)
                throw new CourseStateConflictException("Only a draft course can be published.");

            if (MetadataError(course.Title?.Trim(), MaxTitleLength) is not null
                || MetadataError(course.Description?.Trim()) is not null)
                throw new CourseStateConflictException("Provide a valid title and description before publishing.");

            var lessons = await courses.GetCourseLessonsForUpdateAsync(course.Id, ct);
            var hasCompleteLesson = false;
            foreach (var lesson in lessons)
            {
                // Older drafts may contain flagged lessons with no content; keep them hidden.
                var complete = !string.IsNullOrWhiteSpace(lesson.Title) && !string.IsNullOrWhiteSpace(lesson.Content);
                lesson.IsPublished = complete;
                hasCompleteLesson |= complete;
            }
            if (!hasCompleteLesson)
                throw new CourseStateConflictException("Add at least one lesson with a title and content before publishing.");

            course.Status = CourseStatus.Published;
        }, cancellationToken);

    /// <summary>Archives an owned draft or published course without changing its lessons.</summary>
    public Task<AuthorCourseDto> ArchiveCourseAsync(Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        ChangeOwnedCourseAsync(id, user, (course, _) =>
        {
            if (course.Status is not (CourseStatus.Draft or CourseStatus.Published))
                throw new CourseStateConflictException("Only draft or published courses can be archived.");

            course.Status = CourseStatus.Archived;
            return Task.CompletedTask;
        }, cancellationToken);

    private async Task<AuthorCourseDto> ChangeOwnedCourseAsync(Guid id, ClaimsPrincipal user,
        Func<Course, CancellationToken, Task> change, CancellationToken cancellationToken)
    {
        var authorId = await RequireAuthorAsync(user);
        var result = await courses.ChangeCourseAsync(id, (course, ct) =>
        {
            if (course.AuthorId != authorId)
                throw new ContentAccessDeniedException("You do not have access to manage this course.");

            return change(course, ct);
        }, cancellationToken);

        if (result is null)
            throw new ContentNotFoundException("The course was not found.");

        return ToAuthorDto(result.Id, result.AuthorId, result.Title, result.Description, result.Status);
    }

    private async Task<Guid> RequireAuthorAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true || !Guid.TryParse(users.GetUserId(user), out var id)
            || await users.FindByIdAsync(id.ToString()) is null)
            throw new ContentAccountRequiredException();

        if (!user.IsInRole(AppRoles.Author))
            throw new ContentAccessDeniedException("The Author role is required to manage courses.");

        return id;
    }

    private static (string? Title, string? Description) ValidateMetadata(
        bool hasTitle, string? title, bool hasDescription, string? description)
    {
        var errors = new Dictionary<string, string[]>();
        if (!hasTitle && !hasDescription)
            errors.Add(string.Empty, ["Provide title, description, or both."]);

        title = hasTitle ? title?.Trim() : null;
        description = hasDescription ? description?.Trim() : null;
        if (hasTitle && MetadataError(title, MaxTitleLength) is { } titleError)
            errors.Add("title", [titleError]);
        if (hasDescription && MetadataError(description) is { } descriptionError)
            errors.Add("description", [descriptionError]);
        if (errors.Count != 0)
            throw new AuthorCourseValidationException(errors);

        return (title, description);
    }

    private static string? MetadataError(string? value, int? maxLength = null) => value switch
    {
        null or "" => "Provide a nonblank value.",
        _ when value.Contains('\0') => "The value must not contain a null character.",
        _ when maxLength is int limit && value.Length > limit => $"Use at most {limit} characters after trimming.",
        _ => null
    };

    private static AuthorCourseDto ToAuthorDto(Guid id, Guid authorId, string title, string description, CourseStatus status) =>
        new(id, authorId, title, description, status.ToString());
}
