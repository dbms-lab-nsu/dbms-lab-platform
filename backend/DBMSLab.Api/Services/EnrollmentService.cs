using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Repositories;

namespace DBMSLab.Api.Services;

public sealed class EnrollmentService(
    IEnrollmentRepository enrollments,
    AppDbContext db,
    LearningAccessService access) : IEnrollmentService
{
    public async Task<CourseEnrollment> EnrollAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await access.LockPublishedCourseAsync(courseId, ct);
        var enrollment = new CourseEnrollment { UserId = userId, CourseId = courseId };
        var saved = await enrollments.AddAsync(enrollment, ct);
        await transaction.CommitAsync(ct);
        return saved;
    }

    public Task<IEnumerable<CourseEnrollment>> GetUserEnrollmentsAsync(Guid userId, CancellationToken ct = default)
        => enrollments.GetByUserAsync(userId, ct);
}
