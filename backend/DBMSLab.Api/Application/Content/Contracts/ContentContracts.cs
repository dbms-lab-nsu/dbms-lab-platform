using System.ComponentModel.DataAnnotations;

namespace DBMSLab.Api.Application.Content.Contracts;

/// <summary>Binds optional catalog pagination parameters validated by the API.</summary>
public sealed class CourseCatalogQuery
{
    /// <summary>Gets the one-based page number, from 1 to <see cref="int.MaxValue"/>. Defaults to 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Gets the requested number of courses per page, from 1 to 100. Defaults to 20.</summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>Returns a catalog page together with the pagination values used to select it.</summary>
/// <param name="Items">Published course cards for the page; empty if the catalog or page has no items.</param>
/// <param name="Page">The requested one-based page number.</param>
/// <param name="PageSize">The requested page size, even when fewer items are returned.</param>
/// <param name="TotalCount">The number of published courses before pagination.</param>
public sealed record CourseCatalogDto(
    IReadOnlyList<CourseDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Exposes a visible course's title and description without its child elements.</summary>
public sealed record CourseDto(Guid Id, string Title, string Description)
{
    /// <summary>Gets the fixed element type <c>course</c>.</summary>
    public string Type => "course";
    /// <summary>Gets whether the course is unpublished and requires authorized preview access.</summary>
    public bool RequiresAuthentication { get; init; }
}

/// <summary>Exposes a visible course's navigation tree with ordered modules, topics, and lesson summaries.</summary>
/// <remarks>Lesson content is omitted, while empty modules and topics are retained.</remarks>
public sealed record CourseStructureDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<ModuleDto> Modules)
{
    /// <summary>Gets the fixed element type <c>course</c>.</summary>
    public string Type => "course";
    /// <summary>Gets whether this navigation tree belongs to an unpublished course.</summary>
    public bool RequiresAuthentication { get; init; }
}

/// <summary>Exposes a module's description, sibling order, and topic collection.</summary>
public sealed record ModuleDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<TopicDto> Topics)
{
    /// <summary>Gets the fixed element type <c>module</c>.</summary>
    public string Type => "module";
    /// <summary>Gets whether the parent course requires authorized preview access.</summary>
    public bool RequiresAuthentication { get; init; }
}

/// <summary>Exposes a topic's description, sibling order, and lesson summaries visible to the caller.</summary>
public sealed record TopicDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<LessonSummaryDto> Items)
{
    /// <summary>Gets the fixed element type <c>topic</c>.</summary>
    public string Type => "topic";
    /// <summary>Gets whether the parent course requires authorized preview access.</summary>
    public bool RequiresAuthentication { get; init; }
}

/// <summary>Exposes a visible lesson's identifier, title, and sibling order without its content.</summary>
/// <remarks>The authentication requirement applies to reading lesson content, not this summary.</remarks>
public sealed record LessonSummaryDto(Guid Id, string Title, int SortOrder)
{
    /// <summary>Gets the fixed element type <c>lesson</c>.</summary>
    public string Type => "lesson";
    /// <summary>Gets <see langword="true"/> because the separate lesson content endpoint requires a JWT.</summary>
    public bool RequiresAuthentication => true;

    /// <summary>Gets whether the current caller may read this lesson's content, including authorized previews.</summary>
    /// <remarks>False for guests. Authentication alone does not grant access to unpublished content.</remarks>
    public bool IsAccessible { get; init; }
}

/// <summary>Exposes visible lesson content and the identifiers of its parent topic, module, and course.</summary>
/// <remarks>Content is returned as stored; the API does not render or transform its markup.</remarks>
public sealed record LessonDto(
    Guid Id,
    Guid CourseId,
    Guid ModuleId,
    Guid TopicId,
    string Title,
    int SortOrder,
    string Content)
{
    /// <summary>Gets the fixed element type <c>lesson</c>.</summary>
    public string Type => "lesson";
    /// <summary>Gets <see langword="true"/> even for authenticated callers; this is an access rule, not session state.</summary>
    public bool RequiresAuthentication => true;
}
