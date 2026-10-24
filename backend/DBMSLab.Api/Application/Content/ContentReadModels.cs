using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

public sealed record CourseReadModel(
    Guid Id, string Title, string Description, CourseStatus Status);

public sealed record CourseStructureReadModel(
    Guid Id,
    string Title,
    string Description,
    CourseStatus Status,
    IReadOnlyList<ModuleReadModel> Modules);

public sealed record ModuleReadModel(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<TopicReadModel> Topics);

public sealed record TopicReadModel(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<LessonSummaryReadModel> Lessons);

public sealed record LessonSummaryReadModel(
    Guid Id, string Title, int SortOrder, bool IsPublished);

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
