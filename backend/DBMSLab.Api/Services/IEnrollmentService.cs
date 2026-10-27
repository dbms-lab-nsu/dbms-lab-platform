using DBMSLab.Api.Contracts;
using DBMSLab.Api.Domain;

namespace DBMSLab.Api.Services;

public interface IEnrollmentService
{
    Task<CourseEnrollment> EnrollAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<CourseEnrollment>> GetUserEnrollmentsAsync(Guid userId, CancellationToken ct = default);
}
