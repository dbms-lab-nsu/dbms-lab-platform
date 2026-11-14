using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Services;

public sealed class EnrollmentService(
    IEnrollmentRepository enrollments,
    AppDbContext db) : IEnrollmentService
{
    public async Task<CourseEnrollment> EnrollAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == courseId, ct);
        if (course is null)
            throw new KeyNotFoundException("Course not found.");

        if (course.Status != CourseStatus.Published)
            throw new InvalidOperationException("Course is not published and cannot be started.");

        var existing = await enrollments.GetByUserAndCourseAsync(userId, courseId, ct);
        if (existing is not null)
            return existing;

        var enrollment = new CourseEnrollment { UserId = userId, CourseId = courseId };
        await enrollments.AddAsync(enrollment, ct);
        return enrollment;
    }

    public Task<IEnumerable<CourseEnrollment>> GetUserEnrollmentsAsync(Guid userId, CancellationToken ct = default)
        => enrollments.GetByUserAsync(userId, ct);
}
