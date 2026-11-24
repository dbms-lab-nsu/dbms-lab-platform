using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBMSLab.Api.Repositories.Persistence;

public sealed class EnrollmentRepository(AppDbContext db) : IEnrollmentRepository
{
    public Task<CourseEnrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => db.CourseEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, ct);

    public async Task<IEnumerable<CourseEnrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.CourseEnrollments.AsNoTracking()
            .Include(x => x.Course)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Course.Title)
            .ThenBy(x => x.CourseId)
            .ToListAsync(ct);

    public async Task<CourseEnrollment> AddAsync(CourseEnrollment enrollment, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO course_enrollments ("Id", "UserId", "CourseId", "EnrolledAt")
            VALUES ({enrollment.Id}, {enrollment.UserId}, {enrollment.CourseId}, {enrollment.EnrolledAt})
            ON CONFLICT ("UserId", "CourseId") DO NOTHING
            """, ct);
        return (await GetByUserAndCourseAsync(enrollment.UserId, enrollment.CourseId, ct))!;
    }

    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        => db.CourseEnrollments
            .AnyAsync(x => x.UserId == userId && x.CourseId == courseId, ct);
}
