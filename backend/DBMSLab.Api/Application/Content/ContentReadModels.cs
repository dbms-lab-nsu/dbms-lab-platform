using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

/// <summary>Projects course card fields and the stored status used to decide whether the course is visible.</summary>
public sealed record CourseReadModel(
    Guid Id, string Title, string Description, CourseStatus Status);

/// <summary>Projects a course navigation tree before unpublished lessons are filtered out.</summary>
/// <remarks>Contains publication state and lesson metadata, but no lesson content.</remarks>
public sealed record CourseStructureReadModel(
    Guid Id,
    string Title,
    string Description,
    CourseStatus Status,
    IReadOnlyList<ModuleReadModel> Modules);

/// <summary>Projects a module and its topics in ascending sibling SortOrder.</summary>
public sealed record ModuleReadModel(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<TopicReadModel> Topics);

/// <summary>Projects a topic and its ordered lessons, including unpublished lesson metadata.</summary>
public sealed record TopicReadModel(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<LessonSummaryReadModel> Lessons);

/// <summary>Projects the fields needed to order and filter lessons without loading their content.</summary>
public sealed record LessonSummaryReadModel(
    Guid Id, string Title, int SortOrder, bool IsPublished);

/// <summary>Projects lesson content together with its parent identifiers and publication states.</summary>
/// <remarks>The service must verify both publication states before returning the content to a caller.</remarks>
public sealed record LessonReadModel(
    Guid Id,
    Guid CourseId,
    Guid ModuleId,
    Guid TopicId,
    string Title,
    int SortOrder,
    string Content,
    bool IsPublished,
    CourseStatus CourseStatus);
