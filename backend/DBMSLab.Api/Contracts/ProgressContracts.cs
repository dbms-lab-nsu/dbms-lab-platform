using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Contracts;

/// <summary>Sets the learner's status; the field must be present and contain a named status.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LessonProgressRequest
{
    /// <summary>The required named status; null and omitted values are invalid.</summary>
    [Required]
    [JsonConverter(typeof(LessonProgressStatusConverter))]
    public LessonProgressStatus? Status { get; init; }
}

/// <summary>Accepts named progress states and rejects JSON numbers.</summary>
public sealed class LessonProgressStatusConverter : JsonConverter<LessonProgressStatus>
{
    /// <inheritdoc />
    public override LessonProgressStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            foreach (var status in Enum.GetValues<LessonProgressStatus>())
                if (string.Equals(value, status.ToString(), StringComparison.OrdinalIgnoreCase))
                    return status;
        }

        throw new JsonException("Use NotStarted, InProgress, or Completed.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LessonProgressStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Enum.GetName(value) ?? throw new JsonException("Invalid progress status."));
}

public sealed record LessonProgressResponse(
    Guid LessonId,
    [property: JsonConverter(typeof(LessonProgressStatusConverter))] LessonProgressStatus Status,
    DateTime UpdatedAt);

public sealed record CourseProgressDto(
    Guid CourseId,
    int CompletedLessons,
    int TotalLessons,
    int ProgressPercent);
