using System.ComponentModel.DataAnnotations;

namespace DBMSLab.Api.Application.Content.Contracts;

public sealed class CourseCatalogQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CourseCatalogDto(
    IReadOnlyList<CourseDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CourseDto(Guid Id, string Title, string Description)
{
    public string Type => "course";
    public bool RequiresAuthentication => false;
}

public sealed record CourseStructureDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<ModuleDto> Modules)
{
    public string Type => "course";
    public bool RequiresAuthentication => false;
}

public sealed record ModuleDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<TopicDto> Topics)
{
    public string Type => "module";
    public bool RequiresAuthentication => false;
}

public sealed record TopicDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<LessonSummaryDto> Lessons)
{
    public string Type => "topic";
    public bool RequiresAuthentication => false;
}

public sealed record LessonSummaryDto(Guid Id, string Title, int SortOrder)
{
    public string Type => "lesson";
    public bool RequiresAuthentication => true;
}

public sealed record LessonDto(
    Guid Id,
    Guid CourseId,
    Guid ModuleId,
    Guid TopicId,
    string Title,
    int SortOrder,
    string Content)
{
    public string Type => "lesson";
    public bool RequiresAuthentication => true;
}
