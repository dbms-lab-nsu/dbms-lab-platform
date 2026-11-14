using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Contracts;

public sealed record LessonProgressRequest(
    [Required]
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LessonProgressStatus Status);

public sealed record LessonProgressResponse(
    Guid LessonId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LessonProgressStatus Status,
    DateTime UpdatedAt);

public sealed record CourseProgressDto(
    Guid CourseId,
    int CompletedLessons,
    int TotalLessons,
    int ProgressPercent);
