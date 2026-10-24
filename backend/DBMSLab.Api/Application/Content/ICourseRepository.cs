using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Application.Content;

public interface ICourseRepository
{
    Task<int> CountCoursesAsync(CourseStatus status, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseReadModel>> GetCoursesAsync(
        CourseStatus status, int offset, int limit, CancellationToken cancellationToken);
    Task<CourseReadModel?> GetCourseAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseStructureReadModel?> GetStructureAsync(Guid id, CancellationToken cancellationToken);
    Task<LessonReadModel?> GetLessonAsync(Guid id, CancellationToken cancellationToken);
}
