using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Repositories;

public interface IProgressRepository
{
    Task<LessonProgress?> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);
    Task<IEnumerable<LessonProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<LessonProgress>> GetByCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task AddOrUpdateAsync(LessonProgress progress, CancellationToken ct = default);
}
