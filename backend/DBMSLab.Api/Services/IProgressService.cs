using DBMSLab.Api.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Services;

public interface IProgressService
{
    Task<LessonProgress> UpdateLessonProgressAsync(Guid userId, Guid lessonId, LessonProgressStatus status, CancellationToken ct = default);
    Task<CourseProgressDto> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}
