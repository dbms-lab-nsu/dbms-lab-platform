using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Repositories;

public interface IEnrollmentRepository
{
    Task<CourseEnrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<CourseEnrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    /// <summary>Creates one enrollment or returns the existing enrollment under concurrent requests.</summary>
    Task<CourseEnrollment> AddAsync(CourseEnrollment enrollment, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}
